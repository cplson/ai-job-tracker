import { useNavigate } from "react-router-dom";

interface SubmitButtonProps {
  label?: string;
  isLoading?: boolean;
  className?: string;
  type?: "submit" | "button";
  successState?: string;   // e.g. 'created' | 'updated'
  fallbackPath?: string;   // where to navigate after success
  onClick?: () => Promise<void>; // async handler for forms
}

export default function SubmitButton({
  label = "Save",
  isLoading = false,
  className = "btn btn-primary",
  type = "submit",
  successState,
  fallbackPath,
  onClick,
}: SubmitButtonProps) {
  const navigate = useNavigate();

  const handleClick = async (e: React.MouseEvent<HTMLButtonElement>) => {
    // Prevent native form submission for type="submit"
    if (type === "submit") e.preventDefault();
    
    if (onClick) {
      await onClick();

      // Navigate with success state if specified
      if (fallbackPath && successState) {
        navigate(fallbackPath, { state: { success: successState } });
      }
    }
  };

  return (
    <button
      type={type || "button"}
      className={className}
      disabled={isLoading}
      onClick={handleClick}
    >
      {isLoading ? (
        <>
          <span className="spinner-border spinner-border-sm me-2" role="status" />
          {label}
        </>
      ) : (
        label
      )}
    </button>
  );
}