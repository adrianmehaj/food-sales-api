window.addEventListener('load', function () {
  const apiRoot = new URL('../api/', document.baseURI);
  window.ui = SwaggerUIBundle({
    url: './v1/swagger.json',
    dom_id: '#swagger-ui',
    presets: [SwaggerUIBundle.presets.apis, SwaggerUIStandalonePreset],
    layout: 'StandaloneLayout',
    validatorUrl: null,
    persistAuthorization: false,
    requestInterceptor: async function (request) {
      const target = new URL(request.url, window.location.href);
      if (target.origin !== window.location.origin) throw new Error('Swagger lejon vetëm kërkesa në të njëjtin aplikacion.');
      request.credentials = 'same-origin';
      const method = (request.method || 'GET').toUpperCase();
      if (!['GET', 'HEAD', 'OPTIONS', 'TRACE'].includes(method) && target.pathname.startsWith(apiRoot.pathname)) {
        const response = await fetch(new URL('account/csrf', apiRoot), { credentials: 'same-origin', cache: 'no-store' });
        if (!response.ok) throw new Error('Nuk u mor tokeni antiforgery. Provoni përsëri.');
        const { token } = await response.json();
        request.headers = request.headers || {};
        request.headers['X-CSRF-TOKEN'] = token;
      }
      return request;
    }
  });
});
