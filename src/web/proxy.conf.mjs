const gatewayUrl =
  process.env.API_GATEWAY_URL ??
  process.env.services__api_gateway__http__0 ??
  process.env.services__api_gateway__https__0 ??
  process.env.services__api_gateway ??
  'http://localhost:5293';

export default {
  '/api': {
    target: gatewayUrl,
    changeOrigin: true,
    secure: false,
    xfwd: true,
  },
  '/bff': {
    target: gatewayUrl,
    changeOrigin: true,
    secure: false,
    xfwd: true,
  },
  '/hubs': {
    target: gatewayUrl,
    changeOrigin: true,
    secure: false,
    xfwd: true,
    ws: true,
  },
};
