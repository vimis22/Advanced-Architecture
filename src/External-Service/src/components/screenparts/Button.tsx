import React from "react";

interface ButtonProps {
  onClick?: () => void;
  text: string;
  buttonHeight?: number | string;
  buttonWidth?: number | string;
  buttonColor?: string;
  buttonTextSize?: number;
  buttonTextColor?: string;
  buttonTextWeight?: React.CSSProperties["fontWeight"];
  buttonTextAlign?: React.CSSProperties["textAlign"];
}

const Button: React.FC<ButtonProps> = ({onClick, text, buttonHeight = 40, buttonWidth = 120, buttonColor = "#330099",
  buttonTextSize = 16, buttonTextColor = "#ffffff", buttonTextWeight = "bold", buttonTextAlign = "center"}) => {
  return (
    <button type="button" onClick={onClick} style={{height: buttonHeight, width: buttonWidth, backgroundColor: buttonColor,
          border: "none", borderRadius: 8, cursor: "pointer"}}>
      <span style={{fontSize: buttonTextSize, fontWeight: buttonTextWeight, textAlign: buttonTextAlign,
          color: buttonTextColor, display: "inline-block", width: "100%"}}>
        {text}
      </span>
    </button>
  );
};

export default Button;
