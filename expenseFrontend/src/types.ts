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

export type Budget = {
  weeklyAmount: number | null;
};

export type WeekSummary = {
  weekStart: string;
  weekEnd: string;
  weeklyBudget: number | null;
  spent: number;
  remaining: number | null;
  percentLeft: number | null;
  byCategory: { categoryId: string; categoryName: string; total: number }[];
  fixedPayments: { fixedExpenseId: string; name: string; categoryName: string; date: string; amount: number }[];
};

export type MonthSummary = {
  month: string;
  monthlyBudget: number | null;
  spent: number;
  difference: number | null;
  status: "Green" | "Red" | null;
};

export type FixedFrequency = "Weekly" | "Fortnightly" | "Monthly";

export type FixedExpense = {
  id: string;
  categoryId: string;
  categoryName: string;
  name: string;
  amount: number;
  currency: string;
  startDate: string;
  frequency: FixedFrequency;
  createdAt: string;
};

export type FixedExpenseRequest = {
  name: string;
  amount: number;
  categoryId: string;
  startDate: string;
  frequency: FixedFrequency;
};

export type ReceiptDraft = {
  merchant: string | null;
  amount: number | null;
  currency: string | null;
  expenseDate: string | null;
  categoryId: string | null;
};
