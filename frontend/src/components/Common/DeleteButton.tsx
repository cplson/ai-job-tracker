// import React from "react";

// interface DeleteButtonProps {
//   label?: string;
//   onDelete: () => void; 
//   confirmMessage?: string; 
//   className?: string; 
// }

// export default function DeleteButton({
//   label = "Delete",
//   onDelete,
//   confirmMessage = "Are you sure you want to delete this?",
//   className = "btn btn-danger",
// }: DeleteButtonProps) {
//   const handleClick = () => {
//     if (window.confirm(confirmMessage)) {
//       onDelete();
//     }
//   };

//   return (
//     <button className={className} onClick={handleClick}>
//       {label}
//     </button>
//   );
// }

import { useNavigate } from "react-router-dom";

interface DeleteButtonProps {
  label?: string;
  onDelete: () => Promise<void>; // async delete function
  fallbackPath?: string;          // path to navigate after delete
  successState?: string;          // what to signal to next page
  confirmMessage?: string;
  className?: string;
}

export default function DeleteButton({
  label = "Delete",
  onDelete,
  fallbackPath,
  successState,
  confirmMessage = "Are you sure you want to delete this?",
  className = "btn btn-danger",
}: DeleteButtonProps) {
  const navigate = useNavigate();

  const handleClick = async () => {
    if (!window.confirm(confirmMessage)) return;

    try {
      await onDelete();

      // Navigate to fallbackPath with success state
      if (fallbackPath && successState) {
        navigate(fallbackPath, { state: { success: successState } });
      }
    } catch (err) {
      console.error(err);
      alert("Failed to delete.");
    }
  };

  return (
    <button className={className} onClick={handleClick}>
      {label}
    </button>
  );
}