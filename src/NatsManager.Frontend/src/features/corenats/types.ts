export interface NatsServerInfo {
  serverId: string;
  serverName: string;
  version: string;
  host: string;
  port: number;
  maxPayload: number;
  connections: number;
  inMsgs: number;
  outMsgs: number;
  inBytes: number;
  outBytes: number;
  uptime: string;
  jetStreamEnabled: boolean;
}

export interface NatsSubjectInfo {
  name: string;
  messageCount: number;
}

export interface NatsClientInfo {
  clientId: number;
  name: string;
  address: string;
  subscriptions: number;
}
