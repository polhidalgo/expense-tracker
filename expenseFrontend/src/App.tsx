import { useEffect, useState } from "react";
import { NavLink, Route, Routes } from "react-router-dom";
import { getHealth } from "./api";
import DashboardPage from "./pages/DashboardPage";
import ExpensesPage from "./pages/ExpensesPage";
import ReceiptPage from "./pages/ReceiptPage";

function App() {
  const [status, setStatus] = useState("Checking...");

  useEffect(() => {
    getHealth()
      .then((data) => setStatus(data.status))
      .catch(() => setStatus("Disconnected"));
  }, []);

  return (
    <>
      <header className="app-header">
        <strong>Expense Tracker</strong>
        <nav>
          <NavLink to="/" end>Dashboard</NavLink>
          <NavLink to="/expenses">Expenses</NavLink>
          <NavLink to="/receipt">Receipt</NavLink>
        </nav>
        <span className={status === "Disconnected" ? "status error" : "status"}>
          API: {status}
        </span>
      </header>

      <main>
        <Routes>
          <Route path="/" element={<DashboardPage />} />
          <Route path="/expenses" element={<ExpensesPage />} />
          <Route path="/receipt" element={<ReceiptPage />} />
          <Route path="*" element={<p>Page not found.</p>} />
        </Routes>
      </main>
    </>
  );
}

export default App;
