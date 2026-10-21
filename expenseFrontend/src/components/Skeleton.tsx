type Props = {
  width: number | string;
  height: number | string;
};

// A grey placeholder shown on first load only; refetches keep the old content (§C.11).
export function Skeleton({ width, height }: Props) {
  return <span className="skeleton" style={{ width, height }} aria-hidden="true" />;
}

// Header + 6 rows of 3 bars (60 / 40 / 20 %).
export function TableSkeleton() {
  return (
    <div className="stack" aria-busy="true" aria-label="Loading">
      <Skeleton width="100%" height={36} />
      {Array.from({ length: 6 }, (_, i) => (
        <div key={i} className="actions">
          <Skeleton width="60%" height={14} />
          <Skeleton width="20%" height={14} />
          <Skeleton width="10%" height={14} />
        </div>
      ))}
    </div>
  );
}

// One 200×40 bar + one 160×14 bar.
export function MetricSkeleton() {
  return (
    <div className="stack" aria-busy="true" aria-label="Loading">
      <Skeleton width={200} height={40} />
      <Skeleton width={160} height={14} />
    </div>
  );
}
