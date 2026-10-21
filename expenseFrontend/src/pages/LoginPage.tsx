import { useState, type FormEvent } from "react";
import { takeSessionExpiredMessage } from "../api";
import Alert from "../components/Alert";
import Field from "../components/Field";
import { supabase } from "../supabase";
import { usePageTitle } from "../usePageTitle";

function LoginPage() {
  usePageTitle("Sign in");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [signingIn, setSigningIn] = useState(false);
  // Shown once after the API answered 401.
  const [expiredMessage] = useState(takeSessionExpiredMessage);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSigningIn(true);
    setError("");
    const { error } = await supabase.auth.signInWithPassword({ email, password });
    if (error) setError(error.message);
    setSigningIn(false);
  }

  return (
    <div className="auth">
      <h1>Expense Tracker</h1>
      <p className="muted">Sign in to your account.</p>

      {expiredMessage && !error && <Alert tone="info">{expiredMessage}</Alert>}

      <form className="stack" onSubmit={handleSubmit}>
        <Field id="login-email" label="Email">
          <input
            id="login-email"
            type="email"
            required
            autoComplete="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
          />
        </Field>

        <Field id="login-password" label="Password">
          <input
            id="login-password"
            type="password"
            required
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
        </Field>

        {error && <Alert>{error}</Alert>}

        <button className="btn btn--primary auth__submit" type="submit" disabled={signingIn}>
          {signingIn ? "Signing in…" : "Sign in"}
        </button>
      </form>
    </div>
  );
}

export default LoginPage;
