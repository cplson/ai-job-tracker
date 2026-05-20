interface PaginationProps {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  onPageChange: (page: number) => void;
}

export default function Pagination({
  page,
  pageSize,
  totalCount,
  totalPages,
  onPageChange,
}: PaginationProps) {
  if (totalCount === 0) return null;

  const start = (page - 1) * pageSize + 1;
  const end = Math.min(page * pageSize, totalCount);

  const pages = Array.from({ length: totalPages }, (_, i) => i + 1).filter(p => {
    if (totalPages <= 7) return true;
    return p === 1 || p === totalPages || Math.abs(p - page) <= 1;
  });

  return (
    <div className="d-flex flex-column flex-sm-row justify-content-between align-items-center gap-2 mt-3">
      <p className="text-muted small mb-0">
        Showing {start}–{end} of {totalCount}
      </p>
      <nav aria-label="Pagination">
        <ul className="pagination pagination-sm mb-0">
          <li className={`page-item ${page <= 1 ? 'disabled' : ''}`}>
            <button
              type="button"
              className="page-link"
              disabled={page <= 1}
              onClick={() => onPageChange(page - 1)}
            >
              Previous
            </button>
          </li>
          {pages.map((p, index) => {
            const prev = pages[index - 1];
            const showEllipsis = prev !== undefined && p - prev > 1;
            return (
              <span key={p} className="d-flex">
                {showEllipsis && (
                  <li className="page-item disabled">
                    <span className="page-link">…</span>
                  </li>
                )}
                <li className={`page-item ${p === page ? 'active' : ''}`}>
                  <button type="button" className="page-link" onClick={() => onPageChange(p)}>
                    {p}
                  </button>
                </li>
              </span>
            );
          })}
          <li className={`page-item ${page >= totalPages ? 'disabled' : ''}`}>
            <button
              type="button"
              className="page-link"
              disabled={page >= totalPages}
              onClick={() => onPageChange(page + 1)}
            >
              Next
            </button>
          </li>
        </ul>
      </nav>
    </div>
  );
}
