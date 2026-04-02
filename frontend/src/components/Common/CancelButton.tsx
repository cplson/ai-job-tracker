import { useNavigate } from "react-router-dom";

interface CancelButtonProps {
  label?: string;           // button text
  fallbackPath?: string;    // where to navigate if no history
  className?: string;       // CSS classes
}

export default function CancelButton({
  label = "Cancel",
  fallbackPath = "/",
  className = "btn btn-secondary",
}: CancelButtonProps) {
  const navigate = useNavigate();

  const handleClick = () => {
    // Go back in history if possible
    if (window.history.state?.idx > 0) {
      navigate(-1);
    } else {
      // fallback path if no previous history
      navigate(fallbackPath);
    }
  };

  return (
    <button type="button" className={className} onClick={handleClick}>
      {label}
    </button>
  );
}