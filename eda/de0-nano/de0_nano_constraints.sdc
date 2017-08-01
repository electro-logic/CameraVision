# Author: Leonardo Tazzini (http://electro-logic.blogspot.it)

create_clock -name CLOCK_50 -period 20 [get_ports {CLOCK_50} ]

create_clock -name MIPI_PIXEL_CLK -period 20 [get_ports {MIPI_PIXEL_CLK} ]
create_clock -name MIPI_PIXEL_CLK_EXT -period 20

derive_pll_clocks
derive_clock_uncertainty

# set input and output delays
# -max used for clock setup
# -min used for clock hold
set_input_delay -clock MIPI_PIXEL_CLK_EXT -max 6 [get_ports {MIPI_PIXEL_*} ]
set_input_delay -clock MIPI_PIXEL_CLK_EXT -min 1 [get_ports {MIPI_PIXEL_*} ]