import { useEffect, useState } from "react";
import { NavLink, Route, Routes } from "react-router-dom";
import type { Session } from "@supabase/supabase-js";
import DashboardPage from "./pages/DashboardPage";
import ExpensesPage from "./pages/ExpensesPage";
import LoginPage from "./pages/LoginPage";
import ReceiptPage from "./pages/ReceiptPage";
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

  return (
    <>
      <header className="app-header">
        <strong>Expense Tracker</strong>
        {session && (
          <nav>
            <NavLink to="/" end>Dashboard</NavLink>
            <NavLink to="/expenses">Expenses</NavLink>
            <NavLink to="/receipt">Receipt</NavLink>
          </nav>
        )}
        {session && (
          <span className="status">
            {session.user.email}{" "}
            <button type="button" onClick={() => supabase.auth.signOut()}>
              Sign out
            </button>
          </span>
        )}
      </header>

      <main>
        {checkingSession ? null : !session ? (
          <LoginPage />
        ) : (
          <Routes>
            <Route path="/" element={<DashboardPage />} />
            <Route path="/expenses" element={<ExpensesPage />} />
            <Route path="/receipt" element={<ReceiptPage />} />
            <Route path="*" element={<p>Page not found.</p>} />
          </Routes>
        )}
      </main>
    </>
  );
}

export default App;
