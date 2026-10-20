import type {
  Category,
  DashboardSummary,
  Expense,
  ExpenseListQuery,
  ExpenseRequest,
  FieldErrors,
  PagedResult,
  ReceiptAnalysis,
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
    await supabase.auth.signOut();
    throw new ApiError("Your session has expired. Sign in again.", 401);
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

export const getDashboardSummary = (startDate: string, endDate: string) =>
  request<DashboardSummary>(`/api/dashboard/summary${toQueryString({ startDate, endDate })}`);

export function analyzeReceipt(file: File) {
  const form = new FormData();
  form.append("file", file);
  return request<ReceiptAnalysis>("/api/receipts/analyze", { method: "POST", body: form });
}

export const confirmReceipt = (analysisId: string, body: ExpenseRequest) =>
  request<Expense>("/api/receipts/confirm", {
    method: "POST",
    body: JSON.stringify({ analysisId, ...body }),
  });
