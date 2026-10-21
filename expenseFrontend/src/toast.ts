import { createContext, useContext } from "react";

export type ShowToast = (message: string) => void;

export const ToastContext = createContext<ShowToast>(() => {});

// Shows a short success message, e.g. useToast()("Expense added").
export function useToast(): ShowToast {
  return useContext(ToastContext);
}
