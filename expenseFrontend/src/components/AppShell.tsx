import type { ReactNode } from "react";
import { NavLink } from "react-router-dom";

const DESTINATIONS = [
  { to: "/", label: "This week", short: "Week" },
  { to: "/history", label: "History", short: "History" },
  { to: "/expenses", label: "Expenses", short: "Expenses" },
  { to: "/fixed", label: "Fixed expenses", short: "Fixed" },
  { to: "/receipt", label: "Scan receipt", short: "Scan" },
];

type Props = {
  // Null while the session is being checked: the frame shows without nav (§C.11).
  email: string | null;
  onSignOut: () => void;
  children: ReactNode;
};

// Layout only (§C.1): sidebar on large screens, top bar on medium, top bar + bottom tabs on small.
function AppShell({ email, onSignOut, children }: Props) {
  return (
    <div className="shell">
      <a className="skip-link" href="#content">
        Skip to content
      </a>

      <aside className="shell__bar">
        <span className="shell__wordmark">Expense Tracker</span>
        {email !== null && (
          <>
            <nav className="shell__nav" aria-label="Main">
              {DESTINATIONS.map((d) => (
                <NavLink key={d.to} to={d.to} end={d.to === "/"} className="nav-item">
                  {d.label}
                </NavLink>
              ))}
            </nav>
            <div className="shell__account">
              <span className="shell__email" title={email}>
                {email}
              </span>
              <button className="btn btn--ghost btn--sm" type="button" onClick={onSignOut}>
                Sign out
              </button>
            </div>
          </>
        )}
      </aside>

      <main id="content" className="shell__content" tabIndex={-1}>
        {children}
      </main>

      {email !== null && (
        <nav className="tab-bar" aria-label="Main tabs">
          {DESTINATIONS.map((d) => (
            <NavLink key={d.to} to={d.to} end={d.to === "/"} className="tab-bar__item">
              {d.short}
            </NavLink>
          ))}
        </nav>
      )}
    </div>
  );
}

export default AppShell;
