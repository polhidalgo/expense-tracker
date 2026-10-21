import type {
  Budget,
  Category,
  Expense,
  ExpenseListQuery,
  ExpenseRequest,
  FieldErrors,
  FixedExpense,
  FixedExpenseRequest,
  MonthSummary,
  PagedResult,
  ReceiptDraft,
  WeekSummary,
} from "./types";
import { supabase } from "./supabase";

const API_URL = import.meta.env.VITE_API_URL ?? "http://localhost:5000";

export class ApiError extends Error {
  status: number;
  fieldErrors: FieldErrors;

  constructor(message: string, status: number, fieldErrors: FieldErrors = {}) {
    super(message);
    this.status = status;
    this.fieldErrors = fieldErrors;
  }
}

// The first field error if there is one, otherwise the general message.
export function errorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return Object.values(error.fieldErrors).flat()[0] ?? error.message;
  }
  return "Something went wrong.";
}

const SESSION_EXPIRED = "Your session has expired. Sign in again.";
let sessionExpiredMessage = "";

// The login page shows this once after a 401; reading it clears it.
export function takeSessionExpiredMessage(): string {
  const message = sessionExpiredMessage;
  sessionExpiredMessage = "";
  return message;
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const { data } = await supabase.auth.getSession();
  const headers = new Headers(init?.headers);
  if (data.session) headers.set("Authorization", `Bearer ${data.session.access_token}`);
  // A file upload (FormData) sets its own Content-Type.
  if (!(init?.body instanceof FormData)) headers.set("Content-Type", "application/json");

  let response: Response;
  try {
    response = await fetch(`${API_URL}${path}`, { ...init, headers });
  } catch {
    throw new ApiError("Cannot reach the server.", 0);
  }

  // The session is no longer valid: signing out shows the login page again.
  if (response.status === 401) {
    sessionExpiredMessage = SESSION_EXPIRED;
    await supabase.auth.signOut();
    throw new ApiError(SESSION_EXPIRED, 401);
  }

  if (!response.ok) {
    const problem = await response.json().catch(() => null);
    throw new ApiError(
      problem?.detail ?? problem?.title ?? "Something went wrong.",
      response.status,
      problem?.errors ?? {},
    );
  }

  return response.status === 204 ? (undefined as T) : response.json();
}

function toQueryString(query: ExpenseListQuery): string {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(query)) {
    if (value !== undefined && value !== "") params.set(key, String(value));
  }
  return params.size > 0 ? `?${params}` : "";
}

export const getCategories = () => request<Category[]>("/api/categories");

export const listExpenses = (query: ExpenseListQuery = {}) =>
  request<PagedResult<Expense>>(`/api/expenses${toQueryString(query)}`);

export const getExpense = (id: string) => request<Expense>(`/api/expenses/${id}`);

export const createExpense = (body: ExpenseRequest) =>
  request<Expense>("/api/expenses", { method: "POST", body: JSON.stringify(body) });

export const updateExpense = (id: string, body: ExpenseRequest) =>
  request<Expense>(`/api/expenses/${id}`, { method: "PUT", body: JSON.stringify(body) });

export const deleteExpense = (id: string) =>
  request<void>(`/api/expenses/${id}`, { method: "DELETE" });

export const setBudget = (weeklyAmount: number) =>
  request<Budget>("/api/budget", { method: "PUT", body: JSON.stringify({ weeklyAmount }) });

export const deleteBudget = () => request<void>("/api/budget", { method: "DELETE" });

// The week (Monday to Sunday) that contains the date.
export const getWeekSummary = (date: string) =>
  request<WeekSummary>(`/api/summary/week?date=${encodeURIComponent(date)}`);

// The 12 months ending at endMonth (yyyy-MM), oldest first.
export const getMonthsSummary = (endMonth: string) =>
  request<MonthSummary[]>(`/api/summary/months?endMonth=${encodeURIComponent(endMonth)}`);

export const listFixedExpenses = () => request<FixedExpense[]>("/api/fixed-expenses");

export const createFixedExpense = (body: FixedExpenseRequest) =>
  request<FixedExpense>("/api/fixed-expenses", { method: "POST", body: JSON.stringify(body) });

export const updateFixedExpense = (id: string, body: FixedExpenseRequest) =>
  request<FixedExpense>(`/api/fixed-expenses/${id}`, { method: "PUT", body: JSON.stringify(body) });

export const deleteFixedExpense = (id: string) =>
  request<void>(`/api/fixed-expenses/${id}`, { method: "DELETE" });

export function analyzeReceipt(file: File) {
  const form = new FormData();
  form.append("file", file);
  return request<ReceiptDraft>("/api/receipts/analyze", { method: "POST", body: form });
}
