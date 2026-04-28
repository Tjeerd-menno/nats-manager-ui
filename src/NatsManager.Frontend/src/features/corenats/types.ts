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
  subject: string;
  subscriptions: number;
}

export interface NatsClientInfo {
  clientId: number;
  name: string;
  address: string;
  subscriptions: number;
}

export type PayloadFormat = 'PlainText' | 'Json' | 'HexBytes';

export interface PublishRequest {
  subject: string;
  payload?: string;
  payloadFormat: PayloadFormat;
  headers: Record<string, string>;
  replyTo?: string;
}

export interface NatsLiveMessage {
  subject: string;
  receivedAt: string;
  payloadBase64: string;
  payloadSize: number;
  headers: Record<string, string>;
  replyTo?: string;
  isBinary: boolean;
}
