import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import api from '../../services/api';
import type { ApplicationDto } from '../../types';
import BackButton from '../Common/BackButton';
import DeleteButton from '../Common/DeleteButton';

export default function ApplicationDetails() {
  const { id } = useParams();
  const [application, setApplication] = useState<ApplicationDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const navigate = useNavigate();

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
                <div className="d-flex justify-content-end gap-2 m-3">
                    <button
                        className="btn btn-outline-primary"
                        onClick={() => navigate(`/applications/${application.id}/edit`)}
                    >
                        Edit
                    </button>
                    <DeleteButton
                        label="Delete"
                        fallbackPath='/applications'
                        successState='deleted'
                        onDelete={async () => {
                            try {
                                await api.delete(`/applications/${application.id}`);
                            } catch (err) {
                                console.error(err);
                                alert("Failed to delete application");
                            }
                        }} />
                </div>
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
            <BackButton label="Back to Applications" fallbackPath="/applications" ignoreHistory />
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