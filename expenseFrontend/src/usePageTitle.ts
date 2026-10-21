import { useEffect } from "react";

// "This week · Expense Tracker" in the browser tab.
export function usePageTitle(title: string) {
  useEffect(() => {
    document.title = `${title} · Expense Tracker`;
  }, [title]);
}
