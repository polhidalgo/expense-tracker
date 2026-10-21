import { Link } from "react-router-dom";
import { usePageTitle } from "../usePageTitle";

function NotFoundPage() {
  usePageTitle("Not found");

  return (
    <div className="stack">
      <h1>Page not found</h1>
      <p className="muted">The page you're looking for doesn't exist.</p>
      <div>
        <Link className="btn btn--secondary" to="/">
          Go to this week
        </Link>
      </div>
    </div>
  );
}

export default NotFoundPage;
