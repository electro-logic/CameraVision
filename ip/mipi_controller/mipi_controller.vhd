-- Author: Leonardo Tazzini (http://electro-logic.blogspot.it)

library ieee;
use ieee.std_logic_1164.all;
use ieee.numeric_std.all;

-- Avalon MM (Memory Mapped) slave Interface:

-- Addr	Register	
-- 0 		CONTROL	[RESERVED 30 bit, STATUS, STOP_REQUEST]
-- 1		DMA		[32 bit]

-- STATUS = 1 continue capture
-- STATUS = 0 stop capture

entity mipi_controller is
	port 
	(				
		clk					: in std_logic;
		reset					: in std_logic;							

		-- AVALON SLAVE						
		avs_address 		: in std_logic_vector (7 downto 0);
		avs_write 			: in std_logic;				
		avs_writedata 		: in std_logic_vector (31 downto 0);				
		avs_read 			: in std_logic;		
		avs_readdata 		: out std_logic_vector (31 downto 0);
		
		-- AVALON MASTER
		avm_address			: out std_logic_vector (31 downto 0);		
		avm_write			: out std_logic;
		avm_writedata		: out std_logic_vector (7 downto 0);
		avm_waitrequest	: in std_logic;
		
		-- MIPI
		MIPI_PIXEL_CLK		: in std_logic;							-- Parallel Port Clock signal (20 MHz)
		MIPI_PIXEL_D		: in std_logic_vector(7 downto 0);	-- Parallel Port Data
		MIPI_PIXEL_HS		: in std_logic;							-- Parallel Port Horizontal Synchronization signal		
		MIPI_PIXEL_VS		: in std_logic								-- Parallel Port Vertical Synchronization signal
	);	
end entity;

architecture rtl of mipi_controller is		

	-- Registered signals
	signal MIPI_PIXEL_CLK_R		: std_logic;							
	signal MIPI_PIXEL_D_R		: std_logic_vector(7 downto 0);	
	signal MIPI_PIXEL_HS_R		: std_logic;							
	signal MIPI_PIXEL_VS_R		: std_logic;		
	
	component fifo
		PORT
		(
			clock		: IN STD_LOGIC ;
			data		: IN STD_LOGIC_VECTOR (7 DOWNTO 0);
			rdreq		: IN STD_LOGIC ;
			sclr		: IN STD_LOGIC ;
			empty		: OUT STD_LOGIC ;
			wrreq		: IN STD_LOGIC ;
			full		: OUT STD_LOGIC ;
			q			: OUT STD_LOGIC_VECTOR (7 DOWNTO 0)
		);
	end component;

	signal control_reg 	: std_logic_vector(31 downto 0);
	signal dma_addr_reg 	: std_logic_vector(31 downto 0);
	
	signal addr				: unsigned(31 downto 0) := (others => '0');
	
	-- track pixel old levels
	signal pix_clk			: std_logic;
	signal pix_vs			: std_logic;
	
	signal fifo_rdreq		: std_logic;
	signal fifo_sclr		: std_logic;
	signal fifo_wrreq		: std_logic;
	signal fifo_empty		: std_logic;
	signal fifo_full		: std_logic;
	
	-- Build an enumerated type for the state machine
	type state_avm is (s_avm_idle, s_avm_wait_fifo, s_avm_write);
	-- Register to hold the current state
	signal avm_state 		: state_avm;
		
	signal frame_counter : integer; attribute noprune: boolean; attribute noprune of frame_counter : signal is true;		
	
begin

	--avm_writedata(5 downto 0) <= "000000";

	u_fifo : fifo PORT MAP (
		clock => clk,
		data 	=> MIPI_PIXEL_D_R(7 downto 0),
		rdreq => fifo_rdreq,
		sclr 	=> fifo_sclr,
		wrreq => fifo_wrreq,
		empty => fifo_empty,
		full 	=> fifo_full,
		q 		=> avm_writedata
		-- q 		=> avm_writedata(15 downto 6)
	);
	
	process (clk)
	begin
		if (rising_edge(clk)) then			
			MIPI_PIXEL_CLK_R 	<= MIPI_PIXEL_CLK;
			MIPI_PIXEL_D_R		<= MIPI_PIXEL_D;
			MIPI_PIXEL_HS_R	<= MIPI_PIXEL_HS;
			MIPI_PIXEL_VS_R	<= MIPI_PIXEL_VS;
		end if;
	end process;
	
	-- Avalon Slave Read / Write Control register
	process (clk, reset)		
   begin			

      if reset = '1' then
			control_reg 	<= (others=>'0');
			dma_addr_reg	<= (others=>'0');
      elsif (rising_edge(clk)) then			
		
			-- Read registers
			if avs_read = '1'	then
				case to_integer(unsigned(avs_address)) is
					when 0 		=> avs_readdata <= control_reg;
					when 1		=> avs_readdata <= dma_addr_reg;
					when others => null;
				end case;				
			end if;
				
			-- Write registers
			if avs_write = '1' then
				case to_integer(unsigned(avs_address)) is							
					when 0 => 
						control_reg <= avs_writedata;	
		
						-- there is a new capture request
						--if (avs_writedata = '0') then
						--	fifo_sclr		<= '1';
						--end if;
					when 1 =>
						dma_addr_reg <= avs_writedata;
					when others => null;								
				end case;				
			end if;

			-- Stop if there is a stop request and frame is ended
			if (MIPI_PIXEL_CLK_R = '1' and pix_clk = '0' and MIPI_PIXEL_VS_R = '0') then									
				control_reg(1) <= control_reg(0);										
			end if;			
			
		end if;   
	end process;

	-- read mipi and write fifo
	process(clk, reset)
	begin		
				
		if rising_edge(clk) then				
			if reset = '1' then						
				pix_clk			<= '0';
				pix_vs			<= '0';
				fifo_wrreq		<= '0';
				fifo_sclr		<= '1';
				frame_counter	<= 0;
			else							
				
				fifo_sclr		<= '0';
				fifo_wrreq		<= '0';				
								
				-- rising edge of pixel clock
				if (MIPI_PIXEL_CLK_R = '1' and pix_clk = '0') then
				
					-- Changing frame (falling edge of VS)
					if(MIPI_PIXEL_VS_R = '0' and pix_vs = '1') then					
						frame_counter <= frame_counter + 1;												
						-- addr				<= (others => '0');			
						-- fifo_sclr		<= '1';
					end if;

					-- If can capture
					if control_reg(1) = '0' then					
						-- Pixel of same frame
						if(MIPI_PIXEL_HS_R = '1' and MIPI_PIXEL_VS_R = '1') then			
							fifo_wrreq		<= '1';																					
						end if;								
					end if;
					
				end if; -- end rising edge of pixel clock
									
				pix_clk 	<= MIPI_PIXEL_CLK_R;
				pix_vs	<= MIPI_PIXEL_VS_R;
				
			end if;			
			
		end if;
	
	end process;
	
	-- read fifo and write to slave
	process (clk, reset)
	begin
		if reset = '1' then			
			avm_state 	<= s_avm_idle;	
			addr			<= (others => '0');		
		elsif (rising_edge(clk)) then
			-- Determine the next state synchronously, based on the current state and the input
			case avm_state is
				when s_avm_idle =>
				
					-- Changing frame
					if(MIPI_PIXEL_VS_R = '0') then					
						addr				<= unsigned(dma_addr_reg);
					end if;
					
					if fifo_empty = '0' then
						avm_state <= s_avm_wait_fifo;
					else
						avm_state <= s_avm_idle;
					end if;
					
				when s_avm_wait_fifo =>
					avm_state 			<= s_avm_write;					
				when s_avm_write =>
					if avm_waitrequest = '1' then
						avm_state <= s_avm_write;
					else
						addr			<= addr + 1;
						-- if writing 16 bit increment by 2
						-- addr			<= addr + 2;
						if fifo_empty = '0' then
							avm_state <= s_avm_wait_fifo;
						else
							avm_state <= s_avm_idle;
						end if;					
					end if;								
			end case;
		end if;
	end process;
	
	-- Determine the output based only on the current state and the input (do not wait for a clock edge).
	process (avm_state, addr)		
	begin		
		avm_write		<= '0';
		avm_address		<= std_logic_vector(addr);
		fifo_rdreq		<= '0';
		case avm_state is
			when s_avm_idle 		=> null;
			when s_avm_wait_fifo	=> fifo_rdreq		<= '1';							
			when s_avm_write		=> avm_write		<= '1';
		end case;
	end process;		
	
end rtl;