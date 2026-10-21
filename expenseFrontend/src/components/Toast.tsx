import { useCallback, useEffect, useState, type ReactNode } from "react";
import { ToastContext } from "../toast";

const DISMISS_AFTER_MS = 4000;

// One toast at a time; a new message replaces the current one (§C.11).
function ToastProvider({ children }: { children: ReactNode }) {
  const [toast, setToast] = useState<{ id: number; message: string } | null>(null);

  const show = useCallback((message: string) => setToast({ id: Date.now(), message }), []);

  useEffect(() => {
    if (!toast) return;
    const timer = setTimeout(() => setToast(null), DISMISS_AFTER_MS);
    return () => clearTimeout(timer);
  }, [toast]);

  return (
    <ToastContext.Provider value={show}>
      {children}
      {/* Always mounted so screen readers announce new messages. */}
      <div role="status" aria-live="polite">
        {toast && (
          <div key={toast.id} className="toast">
            {toast.message}
          </div>
        )}
      </div>
    </ToastContext.Provider>
  );
}

export default ToastProvider;
