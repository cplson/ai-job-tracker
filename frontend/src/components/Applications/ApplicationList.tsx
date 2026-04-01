import { useEffect, useState } from 'react';
import api from '../../services/api';
import type { ApplicationDto } from '../../types';

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
      <h2 className="mb-4">My Applications</h2>

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