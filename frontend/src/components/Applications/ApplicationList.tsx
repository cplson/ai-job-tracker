import { useEffect, useMemo, useState } from 'react';
import { useLocation, useNavigate, Link } from 'react-router-dom';
import api from '../../services/api';
import type { ApplicationDto } from '../../types';

const SEARCH_DEBOUNCE_MS = 300;

type SortColumn = 'company' | 'jobTitle' | 'resumeFileName' | 'status';
type SortDirection = 'asc' | 'desc';

function compareApplications(
  a: ApplicationDto,
  b: ApplicationDto,
  column: SortColumn,
  direction: SortDirection
): number {
  let comparison = 0;
  switch (column) {
    case 'company':
      comparison = a.company.localeCompare(b.company, undefined, { sensitivity: 'base' });
      break;
    case 'jobTitle':
      comparison = a.jobTitle.localeCompare(b.jobTitle, undefined, { sensitivity: 'base' });
      break;
    case 'resumeFileName':
      comparison = (a.resumeFileName ?? '').localeCompare(b.resumeFileName ?? '', undefined, {
        sensitivity: 'base',
      });
      break;
    case 'status':
      comparison = a.status.localeCompare(b.status, undefined, { sensitivity: 'base' });
      break;
  }
  return direction === 'asc' ? comparison : -comparison;
}

function matchesSearch(app: ApplicationDto, query: string): boolean {
  const q = query.toLowerCase();
  return (
    app.company.toLowerCase().includes(q) ||
    app.jobTitle.toLowerCase().includes(q) ||
    app.status.toLowerCase().includes(q) ||
    (app.resumeFileName?.toLowerCase().includes(q) ?? false)
  );
}

export default function ApplicationList() {
  const [applications, setApplications] = useState<ApplicationDto[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [debouncedQuery, setDebouncedQuery] = useState('');
  const [sortColumn, setSortColumn] = useState<SortColumn>('company');
  const [sortDirection, setSortDirection] = useState<SortDirection>('asc');
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

    async function fetchApplications() {
      try {
        const res = await api.get<ApplicationDto[]>('/applications/me');
        setApplications(res.data);
      } catch (err) {
        console.error(err);
      }
    }
    fetchApplications();
  }, []);

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedQuery(searchQuery), SEARCH_DEBOUNCE_MS);
    return () => clearTimeout(timer);
  }, [searchQuery]);

  const filteredApplications = useMemo(() => {
    const trimmed = debouncedQuery.trim();
    if (!trimmed) return applications;
    return applications.filter(app => matchesSearch(app, trimmed));
  }, [applications, debouncedQuery]);

  const sortedApplications = useMemo(() => {
    return [...filteredApplications].sort((a, b) =>
      compareApplications(a, b, sortColumn, sortDirection)
    );
  }, [filteredApplications, sortColumn, sortDirection]);

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
          {applications.length === 0 ? (
            <p>No applications yet.</p>
          ) : filteredApplications.length === 0 ? (
            <p>No applications match your search.</p>
          ) : (
            <table className="table table-striped">
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
                {sortedApplications.map(app => (
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
          )}
        </div>
      </div>
    </div>
  );
}