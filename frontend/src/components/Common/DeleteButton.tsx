// components/Common/DeleeButton.tsx
import { useNavigate } from "react-router-dom";
import Button from "./Button";

interface DeleteButtonProps {
  label?: string;
  onDelete: () => Promise<void>;
  fallbackPath?: string;
  successState?: string;
}

export default function DeleteButton({
  label = "Delete",
  onDelete,
  fallbackPath,
  successState,
}: DeleteButtonProps) {
  const navigate = useNavigate();

  const handleDelete = async () => {
    if (!window.confirm("Are you sure you want to delete this?")) return;

    await onDelete();

    if (fallbackPath && successState) {
      navigate(fallbackPath, { state: { success: successState } });
    }
  };

  return (
    <Button
      label={label}
      variant="outline-danger"
      onClick={handleDelete}
    />
  );
}