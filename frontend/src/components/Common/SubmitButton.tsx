import Button from "./Button";

interface SubmitButtonProps {
  label?: string;
  isLoading?: boolean;
}

export default function SubmitButton({
  label = "Save",
  isLoading = false,
}: SubmitButtonProps) {

  return (
    <Button 
      label={label}
      variant="primary"
      isLoading={isLoading}
      type="submit"
    />
  )
}
