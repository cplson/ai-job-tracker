import { useNavigate } from 'react-router-dom';

interface BackButtonProps {
  label?: string;
  fallbackPath?: string;
}

export default function BackButton({
  label = 'Back',
  fallbackPath = '/applications'
}: BackButtonProps) {
  const navigate = useNavigate();

  const handleClick = () => {
    // If browser history exists, go back
    if (window.history.length > 1) {
      navigate(-1);
    } else {
      navigate(fallbackPath);
    }
  };

  return (
    <button className="btn btn-outline-secondary mb-3" onClick={handleClick}>
      ← {label}
    </button>
  );
}