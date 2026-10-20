import { useEffect, useState } from "react";
import { errorMessage, getDashboardSummary } from "../api";
import { formatCurrency, toIsoDate } from "../format";
import type { DashboardSummary } from "../types";

type Period = { startDate: string; endDate: string };

type Result = {
  period: Period;
  data?: DashboardSummary;
  error?: string;
};

function currentMonth(): Period {
  const now = new Date();
  return {
    startDate: toIsoDate(new Date(now.getFullYear(), now.getMonth(), 1)),
    endDate: toIsoDate(new Date(now.getFullYear(), now.getMonth() + 1, 0)),
  };
}

function DashboardPage() {
  const [period, setPeriod] = useState<Period>(currentMonth);
  const [result, setResult] = useState<Result>();

  useEffect(() => {
    if (!period.startDate || !period.endDate) return;
    let ignore = false;
    getDashboardSummary(period.startDate, period.endDate)
      .then((data) => !ignore && setResult({ period, data }))
      .catch((error) => !ignore && setResult({ period, error: errorMessage(error) }));
    return () => {
      ignore = true;
    };
  }, [period]);

  const hasPeriod = Boolean(period.startDate && period.endDate);
  const loading = hasPeriod && result?.period !== period;
  const data = result?.data;
  const currencyTotal = (currency: string) =>
    data?.totals.find((t) => t.currency === currency)?.total ?? 0;

  return (
    <>
      <h1>Dashboard</h1>

      <div className="filters">
        <label>
          From
          <input
            type="date"
            required
            value={period.startDate}
            onChange={(e) => setPeriod({ ...period, startDate: e.target.value })}
          />
        </label>
        <label>
          To
          <input
            type="date"
            required
            value={period.endDate}
            onChange={(e) => setPeriod({ ...period, endDate: e.target.value })}
          />
        </label>
        <button type="button" onClick={() => setPeriod(currentMonth())}>
          This month
        </button>
      </div>

      {!hasPeriod && <p>Choose a start and end date.</p>}

      {loading && <p>Loading summary...</p>}

      {hasPeriod && !loading && result?.error && (
        <p className="error" role="alert">
          {result.error}
        </p>
      )}

      {hasPeriod && !loading && data && data.totals.length === 0 && <p>No expenses in this period.</p>}

      {hasPeriod && !loading && data && data.totals.length > 0 && (
        <>
          <section className="totals">
            {data.totals.map((t) => (
              <div key={t.currency} className="total-card">
                <span className="muted">Total ({t.count} expenses)</span>
                <strong>{formatCurrency(t.total, t.currency)}</strong>
              </div>
            ))}
          </section>

          <h2>By category</h2>
          <table className="expenses">
            <tbody>
              {data.byCategory.map((c) => (
                <tr key={`${c.categoryId}-${c.currency}`}>
                  <td>{c.categoryName}</td>
                  <td className="bar-cell">
                    <div
                      className="bar"
                      style={{ width: `${(c.total / currencyTotal(c.currency)) * 100}%` }}
                    />
                  </td>
                  <td className="amount">{formatCurrency(c.total, c.currency)}</td>
                </tr>
              ))}
            </tbody>
          </table>

          <h2>Recent expenses</h2>
          <table className="expenses">
            <tbody>
              {data.recent.map((e) => (
                <tr key={e.id}>
                  <td>{e.expenseDate}</td>
                  <td>{e.merchant ?? e.description ?? "—"}</td>
                  <td>{e.categoryName}</td>
                  <td className="amount">{formatCurrency(e.amount, e.currency)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </>
      )}
    </>
  );
}

export default DashboardPage;
