interface ButtonProps {
  label: string;
  onClick?: () => void | Promise<void>;
  variant?: "primary" | "secondary" | "danger" | "outline-primary" | "outline-danger";
  isLoading?: boolean;
  type?: "button" | "submit";
  className?: string;
}

export default function Button({
  label,
  onClick,
  variant = "primary",
  isLoading = false,
  type = "button",
  className = "",
}: ButtonProps) {
  return (
    <button
      type={type}
      className={`btn btn-${variant} ${className}`}
      onClick={onClick}
      disabled={isLoading}
    >
      {isLoading ? (
        <>
          <span className="spinner-border spinner-border-sm me-2" />
          {label}
        </>
      ) : (
        label
      )}
    </button>
  );
}