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

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response;
  try {
    // A file upload (FormData) sets its own Content-Type.
    const headers = init?.body instanceof FormData ? init.headers : { "Content-Type": "application/json", ...init?.headers };
    response = await fetch(`${API_URL}${path}`, { ...init, headers });
  } catch {
    throw new ApiError("Cannot reach the server.", 0);
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

export const getHealth = () => request<{ status: string }>("/api/health");

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
