import React from "react";
import { useNavigate } from "react-router-dom";
import Navbar from "../../screenparts/Navbar";
import Button from "../../screenparts/Button";

const HomeScreen: React.FC = () => {
  const navigate = useNavigate();

  function handleCreateOrderClick() {
    navigate("/create-order");
  }

  return (
    <div>
      <Navbar />
      <div style={{ padding: "16px" }}>
        <h1>Home Screen</h1>
        <p>Welcome to the Book Production Portal. Create a new order below.</p>
        <Button
          onClick={handleCreateOrderClick}
          text="Create Order"
          buttonHeight={40}
          buttonWidth={140}
          buttonColor="#330099"
          buttonTextColor="#ffffff"
          buttonTextSize={16}
          buttonTextWeight="bold"
          buttonTextAlign="center"
        />
      </div>
    </div>
  );
};

export default HomeScreen;
