import { useEffect, useMemo, useState } from 'react';
import { useLocation, useNavigate, Link } from 'react-router-dom';
import api from '../../services/api';
import type { ApplicationDto } from '../../types';

const SEARCH_DEBOUNCE_MS = 300;

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
                  <th>Company</th>
                  <th>Job Title</th>
                  <th>Resume</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {filteredApplications.map(app => (
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