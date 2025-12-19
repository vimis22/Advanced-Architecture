import React from "react";

interface NormalTextProps {
  text: string;
  textColor: string;
  fontSize: number;
  fontWeight: React.CSSProperties["fontWeight"];
  textAlign: React.CSSProperties["textAlign"];
  containerHeight?: number | string;
  containerWidth?: number | string;
  containerBackgroundColor?: string;
}

const NormalText: React.FC<NormalTextProps> = (props) => {
  return (
    <div style={{color: props.textColor, fontSize: props.fontSize, fontWeight: props.fontWeight,
        textAlign: props.textAlign, height: props.containerHeight ?? "auto", width: props.containerWidth ?? "auto",
        backgroundColor: props.containerBackgroundColor ?? "transparent", display: "flex", alignItems: "center",
        justifyContent: "center", padding: "8px"}}>
      {props.text}
    </div>
  );
};

export default NormalText;
