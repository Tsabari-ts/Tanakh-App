export interface KpiValue {
  current: number;
  previous: number;
  changePercent: number | null;
}

export interface SmsBalanceResult {
  ok: boolean;
  balance: number | null;
  error: string | null;
}

export interface SmsBalanceInfo extends SmsBalanceResult {
  lowBalanceThreshold: number;
}

export interface AdminOverview {
  totalUsers: KpiValue;
  signupsInRange: KpiValue;
  activeUsers: KpiValue;
  smsBalance: SmsBalanceResult;
  smsSentInRange: KpiValue;
  openErrors: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  limit: number;
}

export interface UserListItem {
  id: string;
  phoneNumber: string | null;
  displayName: string | null;
  status: string;
  createdAt: string;
  updatedAt: string;
}

export interface SmsLogItem {
  id: string;
  maskedPhone: string;
  type: string;
  success: boolean;
  providerStatusCode: number | null;
  createdAt: string;
}

export interface SmsStats {
  sentToday: number;
  sentThisWeek: number;
  sentThisMonth: number;
  successRatePercent: number;
  countByType: Record<string, number>;
}

export interface ErrorLogItem {
  id: string;
  level: string;
  message: string;
  stackTrace: string | null;
  endpoint: string | null;
  statusCode: number | null;
  resolved: boolean;
  createdAt: string;
}

export interface TopError {
  message: string;
  count: number;
}

export type DateRangePreset = 'today' | '7d' | '30d';

export interface DateRange {
  from: string;
  to: string;
}
