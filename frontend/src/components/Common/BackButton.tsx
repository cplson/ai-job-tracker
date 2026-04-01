import { useNavigate } from 'react-router-dom';

interface BackButtonProps {
  label?: string;
  fallbackPath?: string;
  ignoreHistory?: boolean;
}

export default function BackButton({
  label = 'Back',
  fallbackPath = '/',
  ignoreHistory = false
}: BackButtonProps) {
  const navigate = useNavigate();

  const handleClick = () => {
if (ignoreHistory) {
      // always go directly to fallbackPath
      navigate(fallbackPath);
    } else if (window.history.length > 1) {
      // go back in history if possible
      navigate(-1);
    } else {
      // fallback if no history
      navigate(fallbackPath);
    }
  };

  return (
    <button className="btn btn-outline-secondary mb-3" onClick={handleClick}>
      ← {label}
    </button>
  );
}