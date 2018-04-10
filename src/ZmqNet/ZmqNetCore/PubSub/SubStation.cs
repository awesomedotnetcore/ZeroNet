﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Agebull.Common;
using Agebull.Common.Logging;
using Agebull.ZeroNet.Core;
using NetMQ;
using NetMQ.Sockets;

namespace Agebull.ZeroNet.PubSub
{
    /// <summary>
    /// 消息订阅站点
    /// </summary>
    public abstract class SubStation : ZeroStation
    {
        /// <summary>
        /// 订阅主题
        /// </summary>
        public string Subscribe { get; set; } = "";

        private SubscriberSocket _socket;
        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="station"></param>
        public static void Run(SubStation station)
        {
            station.Close();
            station.Config = StationProgram.GetConfig(station.StationName);
            if (station.Config == null)
            {
                StationProgram.WriteError($"{station.StationName} not find,try install...");
                StationProgram.InstallApiStation(station.StationName);
                return;
            }
            station.Run();
        }

        /*// <summary>
        /// 命令处理方法 
        /// </summary>
        public Action<string> ExecFunc { get; set; }

        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual void ExecCommand(string args)
        {
            ExecFunc?.Invoke(args);
        }*/

        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public abstract void Handle(PublishItem args);

        /// <inheritdoc />
        /// <summary>
        /// 命令轮询
        /// </summary>
        /// <returns></returns>
        public sealed override bool Run()
        {
            StationProgram.WriteInfo($"【{StationName}:{RealName}】start...");
            lock (this)
            {
                if (RunState != StationState.Run)
                    RunState = StationState.Start;
                if (_socket != null)
                    return false;
            }
            InPoll = false;

            _socket = new SubscriberSocket();
            try
            {
                _socket.Options.Identity = RealName.ToAsciiBytes();
                _socket.Options.ReconnectInterval = new TimeSpan(0, 0, 0, 0, 200);
                _socket.Subscribe(Subscribe);
                _socket.Connect(Config.InnerAddress);
            }
            catch (Exception e)
            {
                LogRecorder.Exception(e);
                RunState = StationState.Failed;
                CloseSocket();
                StationProgram.WriteError($"【{StationName}:{RealName}】connect error =>{e.Message}");
                return false;
            }
            RunState = StationState.Run;

            _items = MulitToOneQueue<PublishItem>.Load(CacheFileName);
            Task.Factory.StartNew(CommandTask);

            var task1 = new Task(PollTask);
            task1.ContinueWith(OnTaskStop);
            task1.Start();
            while (!InPoll)
                Thread.Sleep(50);
            StationProgram.WriteInfo($"【{StationName}:{RealName}】runing...");
            return true;
        }

        /// <summary>
        /// 缓存文件名称
        /// </summary>
        private string CacheFileName => Path.Combine(StationProgram.Config.DataFolder,
            $"zero_sub_queue_{StationName}.json");

        /// <summary>
        /// 请求队列
        /// </summary>
        private static MulitToOneQueue<PublishItem> _items = new MulitToOneQueue<PublishItem>();

        /// <summary>
        /// 命令轮询
        /// </summary>
        /// <returns></returns>
        private void PollTask()
        {
            StationProgram.WriteLine($"【{StationName}:{RealName}】poll start...");
            var timeout = new TimeSpan(0, 0, 5);
            InPoll = true;
            while (RunState == StationState.Run)
            {
                try
                {
                    if (!_socket.TryReceiveFrameString(timeout, out var title, out var more) || !more)
                    {
                        continue;
                    }
                    if (!_socket.TryReceiveFrameBytes(out var description, out more) || !more)
                    {
                        continue;
                    }
                    PublishItem item = new PublishItem
                    {
                        Title = title
                    };
                    int idx = 1;
                    while (more)
                    {
                        if (!_socket.TryReceiveFrameString(out var val, out more))
                        {
                            continue;
                        }
                        switch (description[idx++])
                        {
                            case ZeroHelper.zero_pub_sub:
                                item.SubTitle = val;
                                break;
                            case ZeroHelper.zero_pub_publisher:
                                item.Station = val;
                                break;
                            case ZeroHelper.zero_arg:
                                item.Content = val;
                                break;
                        }
                    }
                    _items.Push(item);
                }
                catch (Exception e)
                {
                    StationProgram.WriteError($"【{StationName}:{RealName}】poll error{e.Message}...");
                    LogRecorder.Exception(e);
                    RunState = StationState.Failed;
                }
            }
            InPoll = false;
            StationProgram.WriteLine($"【{StationName}:{RealName}】poll stop");
            _items.Save(CacheFileName);
            CloseSocket();
        }
        /// <summary>
        /// 关闭套接字
        /// </summary>
        private void CloseSocket()
        {
            lock (this)
            {
                if (_socket == null)
                    return;
                try
                {
                    _socket.Disconnect(Config.InnerAddress);
                }
                catch (Exception e)
                {
                    LogRecorder.Exception(e);//一般是无法连接服务器而出错
                }
                _socket.Close();
                _socket = null;
            }
        }

        /// <summary>
        /// 命令处理任务
        /// </summary>
        private void CommandTask()
        {
            while (RunState == StationState.Run)
            {
                if (!_items.StartProcess(out var item, 1000))
                    continue;
                Handle(item);
                _items.EndProcess();
            }
        }
    }
}