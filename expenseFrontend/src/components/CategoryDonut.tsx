import { Link } from "react-router-dom";
import { Cell, Pie, PieChart, Tooltip } from "recharts";
import { categoryColor } from "../categoryColors";
import { formatCurrency } from "../format";
import type { WeekSummary } from "../types";

type Props = {
  byCategory: WeekSummary["byCategory"];
  fixedPayments: WeekSummary["fixedPayments"];
  spent: number;
  // The week shown, so each row can open Expenses filtered to it (T19).
  weekStart: string;
  weekEnd: string;
};

const aud = (amount: number) => formatCurrency(amount, "AUD");

// Whole percent; "<1%" for a small non-zero share.
function share(total: number, spent: number): string {
  if (spent <= 0) return "0%";
  const percent = (total / spent) * 100;
  return percent > 0 && percent < 0.5 ? "<1%" : `${Math.round(percent)}%`;
}

const tooltipStyle = {
  background: "var(--surface)",
  border: "none",
  borderRadius: "var(--r-md)",
  boxShadow: "var(--shadow-float)",
  color: "var(--text-h)",
  fontSize: "var(--fs-meta)",
};

// Donut by category (§C.13), with a ranked breakdown list that shows every figure as text.
// Donut and list share the order (amount, descending) and the fixed category colours.
function CategoryDonut({ byCategory, fixedPayments, spent, weekStart, weekEnd }: Props) {
  const rows = [...byCategory].sort((a, b) => b.total - a.total);

  // A category has expenses when its total is more than its fixed payments.
  // Half a cent of margin absorbs floating-point error in the sum.
  const hasExpenses = (categoryName: string, total: number) => {
    const fixedTotal = fixedPayments
      .filter((p) => p.categoryName === categoryName)
      .reduce((sum, p) => sum + p.amount, 0);
    return total - fixedTotal > 0.005;
  };
  const largest = rows[0];
  const label = largest
    ? `Spent ${aud(spent)}; largest category ${largest.categoryName}, ${share(largest.total, spent)}`
    : `Spent ${aud(spent)}`;

  return (
    <div className="donut">
      <div className="donut__chart" role="img" aria-label={label}>
        <PieChart width={168} height={168}>
          <Pie
            data={rows}
            dataKey="total"
            nameKey="categoryName"
            innerRadius="62%"
            outerRadius="100%"
            stroke="var(--bg)"
            strokeWidth={1}
            isAnimationActive={false}
          >
            {rows.map((c) => (
              <Cell key={c.categoryId} fill={categoryColor(c.categoryId)} />
            ))}
          </Pie>
          <Tooltip contentStyle={tooltipStyle} formatter={(value) => aud(Number(value))} />
        </PieChart>
        <div className="donut__centre" aria-hidden="true">
          <strong className="num">{aud(spent)}</strong>
          <span className="muted small">spent</span>
        </div>
      </div>

      <table className="breakdown">
        <thead className="sr-only">
          <tr>
            <th>Category</th>
            <th></th>
            <th>Share</th>
            <th>Amount</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((c) => (
            <tr key={c.categoryId}>
              <td className="breakdown__name">
                {/* Expenses only lists expense rows, so a category with only fixed payments has no link. */}
                {hasExpenses(c.categoryName, c.total) ? (
                  <Link
                    to={`/expenses?${new URLSearchParams({ categoryId: c.categoryId, startDate: weekStart, endDate: weekEnd })}`}
                  >
                    <span className="swatch" style={{ background: categoryColor(c.categoryId) }} />
                    {c.categoryName}
                  </Link>
                ) : (
                  <span className="breakdown__label">
                    <span className="swatch" style={{ background: categoryColor(c.categoryId) }} />
                    {c.categoryName}
                  </span>
                )}
              </td>
              <td className="breakdown__bar" aria-hidden="true">
                <span className="breakdown__track">
                  <span
                    className="breakdown__fill"
                    style={{
                      width: `${spent > 0 ? (c.total / spent) * 100 : 0}%`,
                      background: categoryColor(c.categoryId),
                    }}
                  />
                </span>
              </td>
              <td className="breakdown__share muted small num">{share(c.total, spent)}</td>
              <td className="amount">{aud(c.total)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export default CategoryDonut;
