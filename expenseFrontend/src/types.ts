export type Category = {
  id: string;
  name: string;
};

export type Expense = {
  id: string;
  userId: string;
  categoryId: string;
  categoryName: string;
  amount: number;
  currency: string;
  expenseDate: string;
  merchant: string | null;
  description: string | null;
  createdAt: string;
};

export type ExpenseRequest = {
  amount: number;
  currency?: string;
  expenseDate: string;
  categoryId: string;
  merchant?: string;
  description?: string;
};

export type ExpenseListQuery = {
  startDate?: string;
  endDate?: string;
  categoryId?: string;
  page?: number;
  pageSize?: number;
};

export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
};

export type FieldErrors = Record<string, string[]>;

export type DashboardSummary = {
  startDate: string;
  endDate: string;
  totals: { currency: string; total: number; count: number }[];
  byCategory: { categoryId: string; categoryName: string; currency: string; total: number }[];
  recent: Expense[];
};

export type ReceiptDraft = {
  merchant: string | null;
  amount: number | null;
  currency: string | null;
  expenseDate: string | null;
  categoryId: string | null;
};
