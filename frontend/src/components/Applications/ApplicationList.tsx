import { useEffect, useState } from 'react';
import api from '../../services/api';
import type { ApplicationDto } from '../../types';
import LogoutButton from '../Auth/LogoutButton';

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
      <LogoutButton />
      <h2>My Applications</h2>
      <ul>
        {applications.map(app => (
          <li key={app.id}>
            {app.company} - {app.jobTitle} ({app.status})
          </li>
        ))}
      </ul>
    </div>
  );
}