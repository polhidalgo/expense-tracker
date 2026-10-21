import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Bar, CartesianGrid, Cell, ComposedChart, Line, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import { errorMessage, getMonthsSummary } from "../api";
import Alert from "../components/Alert";
import EmptyState from "../components/EmptyState";
import { TableSkeleton } from "../components/Skeleton";
import { formatCurrency, toIsoDate } from "../format";
import type { MonthSummary } from "../types";
import { usePageTitle } from "../usePageTitle";

const aud = (amount: number) => formatCurrency(amount, "AUD");

// "A$300" on the Y axis.
const audWhole = (amount: number) =>
  new Intl.NumberFormat("en-AU", { style: "currency", currency: "AUD", maximumFractionDigits: 0 }).format(amount);

// "2026-10" → "Oct" (chart) or "October 2026" (table).
function monthLabel(month: string, long = false): string {
  const [year, monthNumber] = month.split("-").map(Number);
  return new Date(year, monthNumber - 1, 1).toLocaleDateString(
    "en-AU",
    long ? { month: "long", year: "numeric" } : { month: "short" },
  );
}

// Same colour as the month's badge: Saved, Over, or no budget.
const barColor = (status: MonthSummary["status"]) =>
  status === "Green" ? "var(--success)" : status === "Red" ? "var(--danger)" : "var(--text-faint)";

const tickStyle = { fill: "var(--text-muted)", fontSize: "var(--fs-meta)" };

const tooltipStyle = {
  background: "var(--surface)",
  border: "none",
  borderRadius: "var(--r-md)",
  boxShadow: "var(--shadow-float)",
  color: "var(--text-h)",
  fontSize: "var(--fs-meta)",
};

function HistoryPage() {
  usePageTitle("History");
  // The frontend sends its own current month, so the server never guesses the timezone.
  const [currentMonth] = useState(() => toIsoDate(new Date()).slice(0, 7));
  const [months, setMonths] = useState<MonthSummary[]>();
  const [error, setError] = useState("");

  useEffect(() => {
    let ignore = false;
    getMonthsSummary(currentMonth)
      .then((data) => !ignore && setMonths(data))
      .catch((e) => !ignore && setError(errorMessage(e)));
    return () => {
      ignore = true;
    };
  }, [currentMonth]);

  const hasBudget = months?.some((m) => m.monthlyBudget !== null) ?? false;
  const hasSpending = months?.some((m) => m.spent > 0) ?? false;
  const chartData = months?.map((m) => ({
    ...m,
    label: monthLabel(m.month) + (m.month === currentMonth ? " (in progress)" : ""),
    short: monthLabel(m.month),
  }));

  return (
    <>
      <div className="page-header">
        <div>
          <h1>History</h1>
          <p className="muted">Last 12 months</p>
        </div>
      </div>

      {error && <Alert>{error}</Alert>}

      {!months && !error && <TableSkeleton />}

      {months && !hasSpending && (
        <EmptyState title="No spending yet" description="Months appear here once you add expenses or fixed expenses." />
      )}

      {months && hasSpending && (
        <>
          {!hasBudget && (
            <p className="muted">
              Months show spending only. <Link to="/">Set a weekly budget</Link> to see what you saved.
            </p>
          )}

          <div
            className="history-chart"
            role="img"
            aria-label="Spent per month against the monthly budget; the same figures are in the table below."
          >
            <ResponsiveContainer width="100%" height="100%">
              <ComposedChart data={chartData}>
                <CartesianGrid vertical={false} stroke="var(--border)" />
                <XAxis dataKey="short" tick={tickStyle} axisLine={false} tickLine={false} />
                <YAxis
                  tickFormatter={(value) => audWhole(Number(value))}
                  tick={tickStyle}
                  axisLine={false}
                  tickLine={false}
                  width={64}
                />
                <Tooltip
                  contentStyle={tooltipStyle}
                  cursor={{ fill: "var(--surface-sunken)" }}
                  labelFormatter={(_, payload) => payload?.[0]?.payload?.label ?? ""}
                  formatter={(value, name) => [aud(Number(value)), name === "spent" ? "Spent" : "Budget"]}
                />
                <Bar dataKey="spent" radius={[2, 2, 0, 0]} isAnimationActive={false}>
                  {months.map((m) => (
                    <Cell
                      key={m.month}
                      fill={barColor(m.status)}
                      fillOpacity={m.month === currentMonth ? 0.5 : 1}
                    />
                  ))}
                </Bar>
                {hasBudget && (
                  <Line
                    dataKey="monthlyBudget"
                    stroke="var(--text-muted)"
                    strokeDasharray="4 4"
                    dot={false}
                    isAnimationActive={false}
                  />
                )}
              </ComposedChart>
            </ResponsiveContainer>
          </div>

          <div className="table-wrap">
            <table className="data-table data-table--history">
              <thead>
                <tr>
                  <th>Month</th>
                  <th className="amount">Budget</th>
                  <th className="amount">Spent</th>
                  <th className="amount">Difference</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {[...months].reverse().map((m) => (
                  <tr key={m.month}>
                    <td className="c-main">{monthLabel(m.month, true)}</td>
                    <td className="amount amount--plain c-budget">{m.monthlyBudget === null ? "—" : aud(m.monthlyBudget)}</td>
                    <td className="amount amount--plain c-spent">{aud(m.spent)}</td>
                    <td className="amount c-amount">
                      {m.difference === null
                        ? "—"
                        : `${m.difference >= 0 ? "+" : "−"}${aud(Math.abs(m.difference))}`}
                    </td>
                    <td className="c-status">
                      <span className="actions">
                        {m.status === "Green" && <span className="badge badge--saved">Saved</span>}
                        {m.status === "Red" && <span className="badge badge--over">Over</span>}
                        {m.month === currentMonth && <span className="muted">In progress</span>}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
    </>
  );
}

export default HistoryPage;
