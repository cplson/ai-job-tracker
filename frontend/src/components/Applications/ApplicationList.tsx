import { useCallback, useEffect, useState } from 'react';
import { useLocation, useNavigate, Link } from 'react-router-dom';
import api from '../../services/api';
import Pagination from '../Common/Pagination';
import type { ApplicationDto, PagedResultDto } from '../../types';

const SEARCH_DEBOUNCE_MS = 300;
const PAGE_SIZE = 10;

type SortColumn = 'company' | 'jobTitle' | 'resumeFileName' | 'status';
type SortDirection = 'asc' | 'desc';

const SORT_API_MAP: Record<SortColumn, string> = {
  company: 'company',
  jobTitle: 'jobtitle',
  resumeFileName: 'resumefilename',
  status: 'status',
};

export default function ApplicationList() {
  const [applications, setApplications] = useState<ApplicationDto[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [debouncedQuery, setDebouncedQuery] = useState('');
  const [sortColumn, setSortColumn] = useState<SortColumn>('company');
  const [sortDirection, setSortDirection] = useState<SortDirection>('asc');
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [loading, setLoading] = useState(true);
  const location = useLocation();
  const navigate = useNavigate();
  const [showSuccess, setShowSuccess] = useState<string | null>(null);

  useEffect(() => {
    const success = location.state?.success;
    if (success) {
      if (success === 'created') setShowSuccess('Application created successfully');
      else if (success === 'updated') setShowSuccess('Application updated successfully');
      else if (success === 'deleted') setShowSuccess('Application deleted successfully');

      navigate(location.pathname, { replace: true, state: {} });
    }
  }, [location, navigate]);

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedQuery(searchQuery), SEARCH_DEBOUNCE_MS);
    return () => clearTimeout(timer);
  }, [searchQuery]);

  useEffect(() => {
    setPage(1);
  }, [debouncedQuery, sortColumn, sortDirection]);

  const fetchApplications = useCallback(async () => {
    setLoading(true);
    try {
      const res = await api.get<PagedResultDto<ApplicationDto>>('/applications/me', {
        params: {
          page,
          pageSize: PAGE_SIZE,
          search: debouncedQuery.trim() || undefined,
          sortBy: SORT_API_MAP[sortColumn],
          sortDescending: sortDirection === 'desc',
        },
      });
      setApplications(res.data.items);
      setTotalCount(res.data.totalCount);
      setTotalPages(res.data.totalPages);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  }, [page, debouncedQuery, sortColumn, sortDirection]);

  useEffect(() => {
    fetchApplications();
  }, [fetchApplications]);

  function handleSort(column: SortColumn) {
    if (sortColumn === column) {
      setSortDirection(d => (d === 'asc' ? 'desc' : 'asc'));
    } else {
      setSortColumn(column);
      setSortDirection('asc');
    }
  }

  function sortIndicator(column: SortColumn) {
    if (sortColumn !== column) return null;
    return sortDirection === 'asc' ? ' ▲' : ' ▼';
  }

  const hasSearch = debouncedQuery.trim().length > 0;

  return (
    <div>
      {showSuccess && (
        <div className="alert alert-success alert-dismissible fade show" role="alert">
          {showSuccess}
          <button
            type="button"
            className="btn-close"
            onClick={() => setShowSuccess(null)}
          ></button>
        </div>
      )}
      <div className="d-flex justify-content-between align-items-center mb-3">
        <h2>My Applications</h2>
        <Link to="/applications/new" className="btn btn-primary">
          + New Application
        </Link>
      </div>

      <div className="mb-3">
        <label htmlFor="application-search" className="form-label visually-hidden">
          Search applications
        </label>
        <input
          id="application-search"
          type="search"
          className="form-control"
          placeholder="Search by company, job title, status, or resume..."
          value={searchQuery}
          onChange={e => setSearchQuery(e.target.value)}
        />
      </div>

      <div className="card">
        <div className="card-body">
          {loading ? (
            <p className="text-muted mb-0">Loading applications...</p>
          ) : totalCount === 0 && !hasSearch ? (
            <p>No applications yet.</p>
          ) : applications.length === 0 ? (
            <p>No applications match your search.</p>
          ) : (
            <>
              <table className="table table-striped mb-0">
                <thead>
                  <tr>
                    {(
                      [
                        ['company', 'Company'],
                        ['jobTitle', 'Job Title'],
                        ['resumeFileName', 'Resume'],
                        ['status', 'Status'],
                      ] as const
                    ).map(([column, label]) => (
                      <th key={column}>
                        <button
                          type="button"
                          className="btn btn-link p-0 text-decoration-none text-dark fw-bold border-0"
                          onClick={e => {
                            e.stopPropagation();
                            handleSort(column);
                          }}
                        >
                          {label}
                          {sortIndicator(column)}
                        </button>
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {applications.map(app => (
                    <tr
                      key={app.id}
                      style={{ cursor: 'pointer' }}
                      onClick={() => navigate(`/applications/${app.id}`)}
                    >
                      <td>{app.company}</td>
                      <td>{app.jobTitle}</td>
                      <td>{app.resumeFileName ?? 'None'}</td>
                      <td>{app.status}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
              <Pagination
                page={page}
                pageSize={PAGE_SIZE}
                totalCount={totalCount}
                totalPages={totalPages}
                onPageChange={setPage}
              />
            </>
          )}
        </div>
      </div>
    </div>
  );
}
