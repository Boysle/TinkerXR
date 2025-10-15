# Docker image name curabase

# Use an unofficial Node.js runtime as the base image
FROM nikolaik/python-nodejs:python3.10-nodejs20

# Set the working directory in the container
WORKDIR /app

# Install necessary packages
RUN apt-get update && apt-get install -y \
    python3-pip git cmake ninja-build efibootmgr build-essential checkinstall \
    libegl-dev zlib1g-dev libssl-dev autoconf libx11-dev libx11-xcb-dev libdbus-1-dev \
    libfontenc-dev libice-dev libsm-dev libxau-dev libxaw7-dev libxcomposite-dev \
    libxcursor-dev libxdamage-dev libxdmcp-dev libxext-dev libxfixes-dev libxi-dev \
    libxinerama-dev libxkbfile-dev libxmu-dev libxmuu-dev libxpm-dev libxrandr-dev \
    libxrender-dev libxres-dev libxss-dev libxt-dev libxtst-dev libxv-dev libxvmc-dev \
    libxxf86vm-dev xtrans-dev libxcb-render0-dev libxcb-render-util0-dev libxcb-xkb-dev \
    libxcb-icccm4-dev libxcb-image0-dev libxcb-keysyms1-dev libxcb-randr0-dev libxcb-shape0-dev \
    libxcb-sync-dev libxcb-xfixes0-dev libxcb-xinerama0-dev xkb-data libxcb-dri3-dev uuid-dev \
    libxcb-glx0-dev libxcb-cursor-dev libxcb-dri2-0-dev libxcb-present-dev libxcb-composite0-dev \
    libxcb-ewmh-dev libxcb-res0-dev libxcb-util-dev libxkbcommon-x11-dev pkg-config flex bison g++-12 gcc-12 \
    && apt-get clean

# Cura

# Clone Cura repository
RUN git clone --branch 5.7.0 --depth=1 https://github.com/Ultimaker/Cura.git
WORKDIR /app/Cura

# Install Conan and Cura dependencies using Conan
RUN pip install conan==1.64 sip==6.8.3 --break-system-packages
RUN conan config install https://github.com/ultimaker/conan-config.git
RUN conan profile new default --detect --force
RUN conan install . --build=missing --update -o cura:devtools=True -g VirtualPythonEnv && conan remove "*" -s -b -f

#RUN cmake --preset release
#RUN cmake --build --preset release

WORKDIR /app

# Create symlinks for executables
RUN ln -s Cura/CuraEngine CuraEngine && \
    ln -s  Cura/curaengine_plugin_gradual_flow curaengine_plugin_gradual_flow && \
    ln -s Cura/curaengine_plugin_gradual_flow /app/Cura/plugins/CuraEngineGradualFlow/x86_64/Linux/curaengine_plugin_gradual_flow

ENV QT_QPA_PLATFORM=offscreen

RUN mkdir output

COPY config/ /root/.config/cura/5.7/
COPY local/ /root/.local/share/cura/5.7/

COPY cura_app.py ./Cura/cura_app.py

# Start the server
CMD bash -c 'source /app/Cura/venv/bin/activate && python3 /app/Cura/cura_app.py --headless --support-enable true --support-structure tree --layer-height 0.3 --infill-sparse-density 40 --infill-pattern cross --output-gcode output/model.gcode output/model.stl'
