import { useEffect, useState } from 'react';
import api from '../../services/api';
import type { ApplicationDto } from '../../types';
import { Link } from 'react-router-dom';

export default function ApplicationList() {
  const [applications, setApplications] = useState<ApplicationDto[]>([]);

  useEffect(() => {
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

  return (
    <div>
      <div className="d-flex justify-content-between align-items-center mb-3">
        <h2>My Applications</h2>
        <Link to="/applications/new" className="btn btn-primary">
          + New Application
        </Link>
      </div>
      
      <div className="card">
        <div className="card-body">
          {applications.length === 0 ? (
            <p>No applications yet.</p>
          ) : (
            <table className="table table-striped">
              <thead>
                <tr>
                  <th>Company</th>
                  <th>Job Title</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {applications.map(app => (
                  <tr key={app.id}>
                    <td>{app.company}</td>
                    <td>{app.jobTitle}</td>
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