import { useNavigate } from "react-router-dom";
import Button from "./Button";

interface EditButtonProps {
  to: string;
}

export default function EditButton({ to }: EditButtonProps) {
  const navigate = useNavigate();

  return (
    <Button
      label="Edit"
      variant="outline-primary"
      onClick={() => navigate(to)}
    />
  );
}