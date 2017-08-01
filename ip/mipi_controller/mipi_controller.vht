LIBRARY ieee;                                               
USE ieee.std_logic_1164.all;                                
use ieee.numeric_std.all;
use std.env.all;

ENTITY mipi_controller_tb IS
END mipi_controller_tb;

ARCHITECTURE rtl OF mipi_controller_tb IS
                                             
	SIGNAL avm_address 		: STD_LOGIC_VECTOR(31 DOWNTO 0);
	SIGNAL avm_waitrequest 	: STD_LOGIC;
	SIGNAL avm_write 			: STD_LOGIC;
	SIGNAL avm_writedata 	: STD_LOGIC_VECTOR(7 DOWNTO 0);
	SIGNAL avs_address 		: STD_LOGIC;
	SIGNAL avs_read 			: STD_LOGIC;
	SIGNAL avs_readdata 		: STD_LOGIC_VECTOR(7 DOWNTO 0);
	SIGNAL avs_write 			: STD_LOGIC;
	SIGNAL avs_writedata 	: STD_LOGIC_VECTOR(7 DOWNTO 0);
	SIGNAL clk 					: STD_LOGIC;
	SIGNAL MIPI_PIXEL_CLK 	: STD_LOGIC;
	SIGNAL MIPI_PIXEL_D 		: STD_LOGIC_VECTOR(9 DOWNTO 0);
	SIGNAL MIPI_PIXEL_HS 	: STD_LOGIC;
	SIGNAL MIPI_PIXEL_VS 	: STD_LOGIC;
	SIGNAL reset 				: STD_LOGIC;
	
	COMPONENT mipi_controller
		PORT (
		avm_address 		: OUT STD_LOGIC_VECTOR(31 DOWNTO 0);
		avm_waitrequest 	: IN STD_LOGIC;
		avm_write 			: OUT STD_LOGIC;
		avm_writedata 		: OUT STD_LOGIC_VECTOR(7 DOWNTO 0);
		avs_address 		: IN STD_LOGIC;
		avs_read 			: IN STD_LOGIC;
		avs_readdata 		: OUT STD_LOGIC_VECTOR(7 DOWNTO 0);
		avs_write 			: IN STD_LOGIC;
		avs_writedata 		: IN STD_LOGIC_VECTOR(7 DOWNTO 0);
		clk 					: IN STD_LOGIC;
		MIPI_PIXEL_CLK 	: IN STD_LOGIC;
		MIPI_PIXEL_D 		: IN STD_LOGIC_VECTOR(9 DOWNTO 0);
		MIPI_PIXEL_HS 		: IN STD_LOGIC;
		MIPI_PIXEL_VS 		: IN STD_LOGIC;
		reset 				: IN STD_LOGIC
		);
	END COMPONENT;

	-- Clock period definitions
   constant mipi_pixel_clk_period 	: time := 40 ns;
	constant clk_period 					: time := 10 ns;
	
BEGIN

	uut : mipi_controller PORT MAP (
		avm_address 		=> avm_address,
		avm_waitrequest 	=> avm_waitrequest,
		avm_write 			=> avm_write,
		avm_writedata 		=> avm_writedata,
		avs_address 		=> avs_address,
		avs_read 			=> avs_read,
		avs_readdata 		=> avs_readdata,
		avs_write 			=> avs_write,
		avs_writedata 		=> avs_writedata,
		clk 					=> clk,
		MIPI_PIXEL_CLK 	=> MIPI_PIXEL_CLK,
		MIPI_PIXEL_D 		=> MIPI_PIXEL_D,
		MIPI_PIXEL_HS 		=> MIPI_PIXEL_HS,
		MIPI_PIXEL_VS 		=> MIPI_PIXEL_VS,
		reset 				=> reset
	);
	
	-- Clock processes
	
   mipi_pixel_clk_process : process
   begin
        MIPI_PIXEL_CLK <= '0';
        wait for mipi_pixel_clk_period/2;
        MIPI_PIXEL_CLK <= '1';
        wait for mipi_pixel_clk_period/2;
   end process;
	
	clk_process : process
   begin
        clk <= '0';
        wait for clk_period/2;
        clk <= '1';
        wait for clk_period/2;
   end process;
   
	-- Stimulus processes	
	
	reset_process : process
	begin
	
		reset 				<= '1';
		wait for 100 ns;		
		reset 				<= '0';
		wait;
		
	end process reset_process;
	
	mipi_process : PROCESS
	BEGIN                
				
		wait until reset = '0';
		
		MIPI_PIXEL_VS		<= '0';
		MIPI_PIXEL_HS		<= '0';
		MIPI_PIXEL_D		<= (others=>'0');
		
		wait for 5*mipi_pixel_clk_period;
		
		MIPI_PIXEL_VS		<= '1';
		MIPI_PIXEL_HS		<= '1';
		for pixel_index in 1 to 10 loop
			MIPI_PIXEL_D		<= std_logic_vector(to_unsigned(pixel_index*4,10));
			wait for mipi_pixel_clk_period;
		end loop;		
		MIPI_PIXEL_VS		<= '0';
		MIPI_PIXEL_HS		<= '0';
		
		wait for 5*mipi_pixel_clk_period;
		stop(0);
		
		WAIT;                                                        
	END PROCESS mipi_process;   

	avs_process : process
	begin
	
		wait until reset = '0';

		avm_waitrequest	<= '0';
		
		-- clear STOP_REQUEST
		avs_address 	<= '0';
		avs_write		<= '1';
		avs_writedata	<= "00000000";
		avs_read			<= '0';		
		avs_readdata	<= "00000000";
		wait for clk_period;
		avs_write		<= '0';
		
		-- Simulate Slave waitrequest
		wait for 9*mipi_pixel_clk_period;
		avm_waitrequest	<= '1';
		wait for 8*mipi_pixel_clk_period;
		avm_waitrequest	<= '0';
		
		wait;
		
	end process avs_process;
                                       
END rtl;
