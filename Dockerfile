#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

#FROM mcr.microsoft.com/dotnet/aspnet:5.0 
FROM mcr.microsoft.com/dotnet/sdk:5.0
ENV DEBIAN_FRONTEND=noninteractive 

ENV OPENCV_VERSION=4.5.3

RUN apt-get update && apt-get install -y libx11-6 libx11-xcb1 libatk1.0-0 libgtk-3-0 libcups2 libdrm2 libxkbcommon0 libxcomposite1 libxdamage1 libxrandr2 libgbm1 libpango-1.0-0 libcairo2 libasound2 libxshmfence1 libnss3 libgdiplus libc6-dev 
RUN ln -s /usr/lib/libgdiplus.so /usr/lib/gdiplus.dll

# Install opencv dependencies
RUN apt-get update && apt-get -y install --no-install-recommends \
      apt-transport-https \
      software-properties-common \
      wget \
      unzip \
      ca-certificates \
      build-essential \
      cmake \
      git \
      libtbb-dev \
      libatlas-base-dev \
      libgtk2.0-dev \
      libavcodec-dev \
      libavformat-dev \
      libswscale-dev \
      libdc1394-22-dev \
      libxine2-dev \
      libv4l-dev \
      libtheora-dev \
      libvorbis-dev \
      libxvidcore-dev \
      libopencore-amrnb-dev \
      libopencore-amrwb-dev \
      libavresample-dev \
      x264 \
      libtesseract-dev \
      libgdiplus \
    && apt-get -y clean \
    && rm -rf /var/lib/apt/lists/*

# Setup opencv and opencv-contrib source
RUN wget https://github.com/opencv/opencv/archive/${OPENCV_VERSION}.zip && \
    unzip ${OPENCV_VERSION}.zip && \
    rm ${OPENCV_VERSION}.zip && \
    mv opencv-${OPENCV_VERSION} opencv && \
    wget https://github.com/opencv/opencv_contrib/archive/${OPENCV_VERSION}.zip && \
    unzip ${OPENCV_VERSION}.zip && \
    rm ${OPENCV_VERSION}.zip && \
    mv opencv_contrib-${OPENCV_VERSION} opencv_contrib

# Build OpenCV
RUN cd opencv && mkdir build && cd build && \
    cmake \
    -D OPENCV_EXTRA_MODULES_PATH=/opencv_contrib/modules \
    -D CMAKE_BUILD_TYPE=RELEASE \
    -D BUILD_SHARED_LIBS=OFF \
    -D ENABLE_CXX11=ON \
    -D BUILD_EXAMPLES=OFF \
    -D BUILD_DOCS=OFF \
    -D BUILD_PERF_TESTS=OFF \
    -D BUILD_TESTS=OFF \
    -D BUILD_JAVA=OFF \
    -D BUILD_opencv_app=OFF \
    -D BUILD_opencv_barcode=OFF \
    -D BUILD_opencv_java_bindings_generator=OFF \
    -D BUILD_opencv_js_bindings_generator=OFF \
    -D BUILD_opencv_python_bindings_generator=OFF \
    -D BUILD_opencv_python_tests=OFF \
    -D BUILD_opencv_ts=OFF \
    -D BUILD_opencv_js=OFF \
    -D BUILD_opencv_bioinspired=OFF \
    -D BUILD_opencv_ccalib=OFF \
    -D BUILD_opencv_datasets=OFF \
    -D BUILD_opencv_dnn_objdetect=OFF \
    -D BUILD_opencv_dpm=OFF \
    -D BUILD_opencv_fuzzy=OFF \
    -D BUILD_opencv_gapi=OFF \
    -D BUILD_opencv_intensity_transform=OFF \
    -D BUILD_opencv_mcc=OFF \
    -D BUILD_opencv_objc_bindings_generator=OFF \
    -D BUILD_opencv_rapid=OFF \
    -D BUILD_opencv_reg=OFF \
    -D BUILD_opencv_stereo=OFF \
    -D BUILD_opencv_structured_light=OFF \
    -D BUILD_opencv_surface_matching=OFF \
    -D BUILD_opencv_videostab=OFF \
    -D BUILD_opencv_wechat_qrcode=OFF \
    -D WITH_GSTREAMER=OFF \
    -D WITH_ADE=OFF \
    -D OPENCV_ENABLE_NONFREE=ON \
    .. && make -j$(nproc) && make install && ldconfig

# Download OpenCvSharp
RUN git clone https://github.com/shimat/opencvsharp.git && cd opencvsharp

# Install the Extern lib.
RUN mkdir /opencvsharp/make && cd /opencvsharp/make && \
    cmake -D CMAKE_INSTALL_PREFIX=/opencvsharp/make /opencvsharp/src && \
    make -j$(nproc) && make install && \
    rm -rf /opencv && \
    rm -rf /opencv_contrib && \
    cp /opencvsharp/make/OpenCvSharpExtern/libOpenCvSharpExtern.so /usr/lib/



ENV ASPNETCORE_URLS=http://+:80

#ENV PUPPETEER_EXECUTABLE_PATH "/usr/bin/chromium-browser"
#�ѵ�ǰĿ¼���Ƶ������ appĿ¼
#COPY . /app
#ָ������Ŀ¼
WORKDIR /app
EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "NETJDC.dll"]