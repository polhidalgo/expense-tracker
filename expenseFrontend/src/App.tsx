import { useEffect, useState } from "react";
import { Route, Routes } from "react-router-dom";
import type { Session } from "@supabase/supabase-js";
import AppShell from "./components/AppShell";
import ExpensesPage from "./pages/ExpensesPage";
import HistoryPage from "./pages/HistoryPage";
import FixedExpensesPage from "./pages/FixedExpensesPage";
import LoginPage from "./pages/LoginPage";
import NotFoundPage from "./pages/NotFoundPage";
import ReceiptPage from "./pages/ReceiptPage";
import WeekPage from "./pages/WeekPage";
import { supabase } from "./supabase";

function App() {
  const [session, setSession] = useState<Session | null>(null);
  const [checkingSession, setCheckingSession] = useState(true);

  useEffect(() => {
    supabase.auth.getSession().then(({ data }) => {
      setSession(data.session);
      setCheckingSession(false);
    });
    const { data } = supabase.auth.onAuthStateChange((_event, newSession) => setSession(newSession));
    return () => data.subscription.unsubscribe();
  }, []);

  const signOut = () => supabase.auth.signOut();

  // While the session is checked, show the empty frame instead of a blank page.
  if (checkingSession) {
    return (
      <AppShell email={null} onSignOut={signOut}>
        {null}
      </AppShell>
    );
  }

  // Signed out, the login view owns the whole page (no shell).
  if (!session) return <LoginPage />;

  return (
    <AppShell email={session.user.email ?? ""} onSignOut={signOut}>
      <Routes>
        <Route path="/" element={<WeekPage />} />
        <Route path="/history" element={<HistoryPage />} />
        <Route path="/expenses" element={<ExpensesPage />} />
        <Route path="/fixed" element={<FixedExpensesPage />} />
        <Route path="/receipt" element={<ReceiptPage />} />
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </AppShell>
  );
}

export default App;
