import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import api from '../../services/api';
import type { ApplicationDto } from '../../types';
import BackButton from '../Common/BackButton';

export default function ApplicationDetails() {
  const { id } = useParams();
  const [application, setApplication] = useState<ApplicationDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    async function fetchApplication() {
      try {
        const res = await api.get<ApplicationDto>(`/applications/${id}`);
        setApplication(res.data);
      } catch (err) {
        console.error(err);
        setError('Failed to load application');
      } finally {
        setLoading(false);
      }
    }

    fetchApplication();
  }, [id]);

  if (loading) return <p>Loading...</p>;
  if (error) return <div className="alert alert-danger">{error}</div>;
  if (!application) return <p>Application not found</p>;

  return (
    <div className="row">
        <div className="col-lg-8">
            <div className="card shadow mb-4">
            <div className="card-body">
                <h3 className="mb-3">{application.jobTitle}</h3>
                <h5 className="text-muted mb-3">{application.company}</h5>

                <span className="badge bg-primary mb-3">
                {application.status}
                </span>

                <hr />

                <h5>Job Description</h5>
                <p style={{ whiteSpace: 'pre-wrap' }}>
                {application.jobDescription || 'No description provided.'}
                </p>
            </div>
            </div>
            <BackButton label="Back to Applications" />
        </div>

        {/* 🔥 Future AI section */}
        <div className="col-lg-4">
            <div className="card shadow">
            <div className="card-body">
                <h5>AI Insights (Coming Soon)</h5>
                <p className="text-muted">
                Resume match score, keyword analysis, and suggestions will appear here.
                </p>
            </div>
            </div>
        </div>
    </div>
  );
}