import React from "react";
import Button from "./Button";

interface NavbarProps {
  onClickMenu?: () => void;
  navbarHeight?: number | string;
  navbarWidth?: number | string;
  navbarBackgroundColor?: string;
}

const Navbar: React.FC<NavbarProps> = ({
  onClickMenu,
  navbarHeight = 60,
  navbarWidth = "100%",
  navbarBackgroundColor = "#222244",
}) => {
  return (
    <div style={{height: navbarHeight, width: navbarWidth, backgroundColor: navbarBackgroundColor, display: "flex",
        alignItems: "center", justifyContent: "space-between", padding: "0 16px", color: "white",}}>
      <div>Book Production</div>
      <Button onClick={onClickMenu} text="Menu" buttonHeight={30} buttonWidth={80} buttonColor="#330099"
              buttonTextColor="#ffffff" buttonTextSize={14} buttonTextWeight="bold" buttonTextAlign="center" />
    </div>
  );
};

export default Navbar;
