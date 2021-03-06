﻿namespace ZeroMQ
{
    /// <summary>
    /// Socket默认配置
    /// </summary>
    public class SocketOption
    {
        /// <summary>
        /// 重新连接时间间隔
        /// </summary>
        public int ReconnectIvl { get; set; } = 10;
        /// <summary>
        /// 重新连接失败时长
        /// </summary>
        public int ReconnectIvlMax { get; set; } = 500;
        /// <summary>
        /// 连接失败时长
        /// </summary>
        public int ConnectTimeout { get; set; } = 500;
        /// <summary>
        /// 自动关闭时长
        /// </summary>
        public int Linger { get; set; } = 200;
        /// <summary>
        /// 接收超时时长
        /// </summary>
        public int RecvTimeout { get; set; } = 5000;
        /// <summary>
        /// 发送超时时长
        /// </summary>
        public int SendTimeout { get; set; } = 5000;
        /// <summary>
        /// 连接队列数量
        /// </summary>
        public int Backlog { get; set; } = 50000;
        /// <summary>
        /// 心跳间隔时长
        /// </summary>
        public int HeartbeatIvl { get; set; } = 1000;
        /// <summary>
        /// 心跳超时时长
        /// </summary>
        public int HeartbeatTimeout { get; set; } = 200;
        /// <summary>
        /// 心跳TTL
        /// </summary>
        public int HeartbeatTtl { get; set; } = 200;
        /// <summary>
        /// 启用Keeplive
        /// </summary>
        public int TcpKeepalive { get; set; } = 1;
        /// <summary>
        /// TCP发送keepalive消息的频度,单位秒
        /// </summary>
        public int TcpKeepaliveIdle { get; set; } = 4096;
        /// <summary>
        ///当探测没有确认时，重新发送探测的频度,单位秒
        /// </summary>
        public int TcpKeepaliveIntvl { get; set; } = 4096;
    }
}