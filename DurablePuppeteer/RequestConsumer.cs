﻿using PuppeteerSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DurablePuppeteer
{
    public static class RequestConsumer
    {
        internal static readonly System.Text.RegularExpressions.Regex RegexDenyTracking = new System.Text.RegularExpressions.Regex("(api.mixpanel.com|liveperson.net|api.segment.io)", System.Text.RegularExpressions.RegexOptions.Compiled);
        internal static readonly System.Text.RegularExpressions.Regex RegexBlockMeta = new System.Text.RegularExpressions.Regex("/gurux/$");
        private static BlockingCollection<Request> _incoming = new BlockingCollection<Request>();
        private static bool _running = false;

        public static bool IsRunning { get { return _running; } }

        public static void Start()
        {
            Task.Run(() =>
            {
                Consume();
            });
            Task.Run(() =>
            {
                Consume();
            });
            _running = true;
        }

        public static void Stop()
        {
            _running = false;
        }

        private static async void Consume()
        {
            while (_running)
            {
                try
                {
                    if (_incoming.TryTake(out Request e))
                    {
                        var meta = RegexBlockMeta.Match(e.Url);

                        if (meta.Success)
                        {
                            var guruxcontent = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String("PCFET0NUWVBFIGh0bWwgPgo8aHRtbCBsYW5nPSJlbiI+CjxoZWFkPgogICA8dGl0bGU+UHJvcGVydHkgR3VydSAtIHRoZSB0cnVzdGVkIHNvdXJjZTwvdGl0bGU+CiAgIDxtZXRhIGh0dHAtZXF1aXY9IkNvbnRlbnQtVHlwZSIgY29udGVudD0idGV4dC9odG1sOyBjaGFyc2V0PUlTTy04ODU5LTEiIC8+CiAgICAgICAgPGJhc2UgaHJlZj0iLy93d3cucHJvcGVydHktZ3VydS5jby5uei9jb250ZW50LyI+Cgk8IS0tbGluayByZWw9InN0eWxlc2hlZXQiIGhyZWY9ImNzcy9wcm9wZXJ0eWd1cnVfbmV3X2xvZ2luLmNzcyIgdHlwZT0idGV4dC9jc3MiLS0+Cgk8IS0tbGluayByZWw9InN0eWxlc2hlZXQiIGhyZWY9ImNzcy9tYWluLmNzcyIgdHlwZT0idGV4dC9jc3MiLS0+Cgk8bGluayByZWw9InN0eWxlc2hlZXQiIGhyZWY9Ii9jb250ZW50L2Nzcy9ib290c3RyYXAubWluLmNzcyIgdHlwZT0idGV4dC9jc3MiPgoJPGxpbmsgcmVsPSJzdHlsZXNoZWV0IiBocmVmPSIvY29udGVudC9jc3MvZ3VydXN0cmFwLmNzcyIgdHlwZT0idGV4dC9jc3MiPgoKCTxsaW5rIHJlbD0ic2hvcnRjdXQgaWNvbiIgaHJlZj0iZmF2aWNvbi5pY28iIHR5cGU9ImltYWdlL3gtaWNvbiI+Cgk8bGluayByZWw9Imljb24iIGhyZWY9ImZhdmljb24uaWNvIiB0eXBlPSJpbWFnZS94LWljb24iPgoJPGxpbmsgcmVsPSJhcHBsZS10b3VjaC1pY29uIiBocmVmPSJhcHBsZS10b3VjaC1pY29uLnBuZyIgLz4KCiAgICAKCTxzY3JpcHQgdHlwZT0idGV4dC9qYXZhc2NyaXB0Ij4KCS8vIHNlY3VyZSByZWRpcmVjdCBmb3IgcHJvZHVjdGlvbiBib3hlcyBvbmx5Cgl2YXIgaCA9IHdpbmRvdy5sb2NhdGlvbi5ob3N0bmFtZTsKCXZhciBwID0gd2luZG93LmxvY2F0aW9uLnByb3RvY29sOwoJaWYocCA9PSAnaHR0cDonICYmIChoID09ICd3d3cucHJvcGVydHktZ3VydS5jby5ueicgfHwgaCA9PSAncHJvcGVydHktZ3VydS5jby5ueicpKSB7IHdpbmRvdy5sb2NhdGlvbi5ocmVmID0gJ2h0dHBzOi8vd3d3LnByb3BlcnR5LWd1cnUuY28ubnovZ3VydXgvJzsgfQoJPC9zY3JpcHQ+Cgk8c2NyaXB0IHR5cGU9InRleHQvamF2YXNjcmlwdCIgc3JjPSIvY29udGVudC9qcy9taWNyb2FqYXguanMiPjwvc2NyaXB0PgoJPHNjcmlwdCB0eXBlPSJ0ZXh0L2phdmFzY3JpcHQiIHNyYz0iL2NvbnRlbnQvanMvc3RhbmRhbG9uZV9sb2dpbi5qcyI+PC9zY3JpcHQ+CgkKPC9oZWFkPgo8Ym9keT4KCjxkaXYgY2xhc3M9Im5hdmJhciI+Cgk8ZGl2IGNsYXNzPSJuYXZiYXItaW5uZXIiPgoJCTxkaXYgY2xhc3M9ImNvbnRhaW5lciI+CgkJCTxhIGNsYXNzPSJicmFuZCIgaHJlZj0iIj4KCQkJCTxpbWcgc3JjPScvZ3VydXgvY3NzL2ltYWdlcy9Qcm9wZXJ0eUd1cnVfQnlDb3JlTG9naWMucG5nJyBhbHQ9J3Byb3BlcnR5Z3VydScgLz4KCQkJPC9hPgoJCQk8YSBocmVmPSIvY29udGVudC9mb3Jtcy9mcmVlX3RyaWFsIiBjbGFzcz0iYnRuIGJ0bi1wcm9tbyBwdWxsLXJpZ2h0Ij5GcmVlIFRyaWFsPC9hPgogIDx1bCBpZD0ibWVudSIgY2xhc3M9Im5hdiBuYXYtcGlsbHMgcHVsbC1yaWdodCI+CiAgICA8bGkgaWQ9ImNtc19zZWN0aW9uXzgiIGNsYXNzPSJkZXB0aC0xIGZpcnN0Ij4KICAgICAgPGEgaHJlZj0iL2NvbnRlbnQvcmVnaXN0ZXJlZC1yZWFsZXN0YXRlLWFnZW50cyI+UmVhbCBFc3RhdGUgQWdlbnRzPC9hPgogICAgPC9saT4KICAgIDxsaSBpZD0iY21zX3NlY3Rpb25fNyIgY2xhc3M9ImRlcHRoLTEiPgogICAgICA8YSBocmVmPSIvY29udGVudC9yZWdpc3RlcmVkLXZhbHVlcnMiPlZhbHVlcnM8L2E+CiAgICA8L2xpPgogICAgPGxpIGlkPSJjbXNfc2VjdGlvbl85IiBjbGFzcz0iZGVwdGgtMSI+CiAgICAgIDxhIGhyZWY9Ii9jb250ZW50L3Byb3BlcnR5LWRldmVsb3BlcnMiPkRldmVsb3BlcnM8L2E+CiAgICA8L2xpPgogICAgPGxpIGlkPSJjbXNfc2VjdGlvbl8xMCIgY2xhc3M9ImRlcHRoLTEiPgogICAgICA8YSBocmVmPSIvY29udGVudC9wcm9wZXJ0eS1pbnZlc3RvcnMiPkludmVzdG9yczwvYT4KICAgIDwvbGk+CiAgICA8bGkgaWQ9ImNtc19wYWdlXzUiIGNsYXNzPSJkZXB0aC0xIj4KICAgICAgPGEgaHJlZj0iL2NvbnRlbnQvbmV3cyI+TmV3czwvYT4KICAgIDwvbGk+CiAgICA8bGkgaWQ9ImNtc19zZWN0aW9uXzExIiBjbGFzcz0iZGVwdGgtMSI+CiAgICAgIDxhIGhyZWY9Ii9jb250ZW50L2hlbHAiPldlYmluYXJzPC9hPgogICAgPC9saT4KICAgIDxsaSBpZD0iY21zX3BhZ2VfMzAiIGNsYXNzPSJkZXB0aC0xIGxhc3QiPgogICAgICA8YSBocmVmPSIvY29udGVudC9wcmljaW5nIj5QcmljaW5nPC9hPgogICAgPC9saT4KICA8L3VsPgoKCQk8L2Rpdj4KCTwvZGl2Pgo8L2Rpdj4KPGRpdiBjbGFzcz0iaG9tZXBhZ2UiPgo8ZGl2IGNsYXNzPSJyb3ciPgogIDxkaXYgY2xhc3M9ImNvbnRhaW5lciI+CiAgICA8ZGl2IGNsYXNzPSJyb3ciPgoJPGRpdiBjbGFzcz0nc3BhbjgnPgoJCSZuYnNwOwo8bm9zY3JpcHQgaWQ9Im5vamF2YXNjcmlwdCIgPgogICAgPGRpdiBjbGFzcz0nYWxlcnQgYWxlcnQtZXJyb3InIHN0eWxlPSJtYXJnaW4tdG9wOjEwcHg7Ij4KICAgIDxwPgogICAgICA8Yj5Zb3UgZG8gbm90IGhhdmUgSmF2YXNjcmlwdCBlbmFibGVkIGluIHlvdXIgYnJvd3Nlci48L2I+PGJyPgogICAgICBKYXZhc2NyaXB0IG11c3QgYmUgZW5hYmxlZCBpbiB5b3VyIGJyb3dzZXIgdG8gbG9naW4gdG8gcHJvcGVydHlndXJ1LgogICAgPC9wPgogICAgPHA+CiAgICAgUGxlYXNlIGNvbnN1bHQgdGhlIGhlbHAgc2VjdGlvbiBvZiB5b3UgYnJvd3NlciBmb3IgaW5zdHJ1Y3Rpb25zIG9uIHJlLWVuYWJsaW5nIEphdmFzY3JpcHQuPGJyPgogICAgIEZvciBmdXJ0aGVyIGFzc2lzdGFuY2UgPGEgaHJlZj0ibWFpbHRvOmluZm9AY29yZWxvZ2ljLmNvLm56Ij5lbWFpbCB1czwvYT4gb3IgcGhvbmUgb3VyIGN1c3RvbWVyIHN1cHBvcnQgdGVhbSBvbiAwODAwIDM1NSAzNTUKICAgIDwvcD4KCiAgPC9kaXY+Cjwvbm9zY3JpcHQ+CgoKCgk8L2Rpdj4KCTxkaXYgaWQ9ImxvZ2luQm94IiBjbGFzcz0nc3BhbjQgbG9naW4nPgoJCTxmb3JtIG5hbWU9ImxvZ29uRm9ybSIgaWQ9ImxvZ2luRm9ybSIgY2xhc3M9ImZvcm0tdmVydGljYWwiIG1ldGhvZD0icG9zdCI+CiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgPGlucHV0IHR5cGU9ImhpZGRlbiIgbmFtZT0ibG9naW4iIHZhbHVlPSIxIiAvPgogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIDxpbnB1dCB0eXBlPSJoaWRkZW4iIG5hbWU9InZhbFJlZiIgdmFsdWU9IiIgLz4KICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICA8aW5wdXQgdHlwZT0iaGlkZGVuIiBuYW1lPSJwYXJjZWxJZCIgdmFsdWU9IiIgLz4KICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICA8aW5wdXQgdHlwZT0iaGlkZGVuIiBuYW1lPSJ0aXRsZVJlZiIgdmFsdWU9IiIgLz4KICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICA8aW5wdXQgdHlwZT0iaGlkZGVuIiBuYW1lPSJsYW5kRGlzdHJpY3RDb2RlIiB2YWx1ZT0iIiAvPgoJCQkgICAgICAgIAoJCQkJCgkJCQk8ZGl2IGNsYXNzPSdjb250cm9sLWdyb3VwICc+CiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCTxsYWJlbCBmb3I9InVzZXIiPkxvZ2luL2VtYWlsIGFkZHJlc3M6PC9sYWJlbD4KICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAJPGlucHV0IHR5cGU9InRleHQiIGlkPSJ1c2VyIiBhdXRvY2FwaXRhbGl6ZT0ib2ZmIiBhdXRvY29ycmVjdD0ib2ZmIiBjbGFzcz0idGV4dHdpZHRoIiBuYW1lPSJ1c2VyIiB2YWx1ZT0iIiAvPgoJCQkJPC9kaXY+CgkJCQk8ZGl2IGNsYXNzPSdjb250cm9sLWdyb3VwICc+CiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCTxsYWJlbCBmb3I9InBhc3N3b3JkIj5QYXNzd29yZDo8L2xhYmVsPgogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAk8aW5wdXQgdHlwZT0icGFzc3dvcmQiIGNsYXNzPSJ0ZXh0d2lkdGgiIG5hbWU9InBhc3N3b3JkIiB2YWx1ZT0iIiAvPgoJCQkJPC9kaXY+CiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgPGxhYmVsIGZvcj0icmVtZW1iZXJQYXNzd29yZCIgY2xhc3M9ImNoZWNrYm94IHB1bGwtbGVmdCIgc3R5bGU9ImZvbnQtc2l6ZToxMHB4OyI+CiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICA8aW5wdXQgdHlwZT0iY2hlY2tib3giIGlkPSJyZW1lbWJlclBhc3N3b3JkIiBuYW1lPSJyZW1lbWJlclBhc3N3b3JkIiB2YWx1ZT0ib24iPktlZXAgbWUgbG9nZ2VkIGluCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgPC9sYWJlbD4KCQkJCTxkaXYgY2xhc3M9ImNvbnRyb2wtZ3JvdXAiIHN0eWxlPSd0ZXh0LWFsaWduOnJpZ2h0Oyc+CiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCTxpbnB1dCB0eXBlPSJzdWJtaXQiIHZhbHVlPSIgICBMb2dpbiAgICIgbmFtZT0iTG9naW4iIGNsYXNzPSdidG4gYnRuLXByaW1hcnknIC8+PGJyIC8+CiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCTxhIGhyZWY9IiMiIG9uY2xpY2s9InNob3dGb3Jnb3RQYXNzd29yZCgpO3JldHVybiBmYWxzZTsiPgogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCUZvcmdvdCB5b3VyIHBhc3N3b3JkPwogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAk8L2E+CgkJCQk8L2Rpdj4KICAgICAgICAgICAgICAgICAgICAgICAgPC9mb3JtPgoJPC9kaXY+Cgk8ZGl2IGlkPSJmb3Jnb3RCb3giIGNsYXNzPSJzcGFuNCBsb2dpbiIgc3R5bGU9ImRpc3BsYXk6bm9uZTsiPgoJICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICA8Zm9ybSBuYW1lPSJmb3Jnb3Rwd2QiIGNsYXNzPSJmb3JtLXZlcnRpY2FsIiBpZD0iZm9yZ290cHdkIiBtZXRob2Q9InBvc3QiPgogICAgICAgICAgICAgICAgICAgICAgICA8bGFiZWwgZm9yPSJ1c2VybmFtZSI+TG9naW4vRW1haWwgYWRkcmVzczogPC9sYWJlbD4KICAgICAgICAgICAgICAgICAgICAgICAgPGlucHV0IHR5cGU9InRleHQiIGlkPSJ1c2VybmFtZSIgY2xhc3M9InRleHR3aWR0aCIgbmFtZT0idXNlcm5hbWUiIG1heGxlbmd0aD0iMTAwIiBzaXplPSIzMCIgLz4KICAgICAgICAgICAgICAgICAgICAgICAgPGlucHV0IGNsYXNzPSJidG4gYnRuLXByaW1hcnkiIHR5cGU9InN1Ym1pdCIgaWQ9InNibUZvcmdvdFB3ZCIgdmFsdWU9IlJlc2V0IFBhc3N3b3JkIiBvbmNsaWNrPSJmb3Jnb3RQd2QoKTtyZXR1cm4gZmFsc2U7IiA+CgkJCTxhIGhyZWY9IiIgb25jbGljaz0iaGlkZUZvcmdvdFBhc3N3b3JkKCk7cmV0dXJuIGZhbHNlOyI+R28gdG8gbG9naW48L2E+CgkJCTxkaXY+CiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgWW91ciBuZXcgcGFzc3dvcmQgd2lsbCBiZSBlbWFpbGVkIHRvIHRoZSBlbWFpbCBhZGRyZXNzIHJlZ2lzdGVyZWQgYWdhaW5zdCB5b3VyIFByb3BlcnR5IEd1cnUgYWNjb3VudC4KICAgICAgICAgICAgICAgICAgICAgICAgPC9kaXY+CiAgICAgICAgICAgICAgICA8L2Zvcm0+CiAgICAgICAgICAgICAgICAgICAgICAgIAk8L2Rpdj4KCTxkaXYgaWQ9InNlbnRQYXNzd29yZEJveCIgY2xhc3M9InNwYW40IGxvZ2luIiBzdHlsZT0iZGlzcGxheTpub25lOyI+UGFzc3dvcmQgU2VudDwvZGl2PgogICAgPC9kaXY+CiAgPC9kaXY+CjwvZGl2Pgo8L2Rpdj4KCjxkaXYgY2xhc3M9ImNvbnRhaW5lciI+Cgo8ZGl2IGNsYXNzPSJyb3cgY21zIj4KCQoJPGRpdiBjbGFzcz0nc3BhbjgnIHN0eWxlPSJtYXJnaW4tdG9wOjE1cHg7Ij4KICAgICAgICA8aDI+SWYgeW91J3JlIHNlcmlvdXMgYWJvdXQgcmVhbCBlc3RhdGUsIHlvdSBuZWVkIGEgc2VyaW91cyBzb2x1dGlvbi48L2gyPgo8cD4KPGJyPgoJUHJvcGVydHkgR3VydSBieSBUZXJyYWxpbmssIGlzIHRoZSBhd2FyZCB3aW5uaW5nLCBtYXJrZXQgbGVhZGluZywgb25saW5lIGFwcGxpY2F0aW9uIDxzcGFuIHN0eWxlPSJmb250LWZhbWlseTogJnF1b3Q7QXJpYWwmcXVvdDssJnF1b3Q7c2Fucy1zZXJpZiZxdW90OzsiIGxhbmc9IkVOIj50aGF0IGdpdmVzIHlvdSBpbnN0YW50IGFjY2VzcyB0byBOZXcgWmVhbGFuZCdzIG1vc3QgdXAtdG8tZGF0ZSBhbmQgYWNjdXJhdGUgcHJvcGVydHkgaW5mb3JtYXRpb24uPC9zcGFuPjwvcD4KPHA+CglJbm5vdmF0aXZlIGluIGRlc2lnbiBhbmQgZnVuY3Rpb24sIGl0IGlzIHRoZSBzZXJpb3VzIHNvbHV0aW9uIG9mIGNob2ljZSBmb3IgTmV3IFplYWxhbmQncyBsZWFkaW5nIHByb3BlcnR5IHByb2Zlc3Npb25hbHMuPC9wPgoKCgkJPGRpdiBjbGFzcz0icm93IGNvbHJvdyI+CgkJCTxkaXYgY2xhc3M9ImNvbDQiPjxkaXYgY2xhc3M9ImJvcmRlcmVkIj4KCQkJCQogICAgPGRpdiBjbGFzcz0iYnViYmxlLWJsdWUiPgoJPGEgaHJlZj0iL2NvbnRlbnQvcmVnaXN0ZXJlZC1yZWFsZXN0YXRlLWFnZW50cyI+TGljZW5zZWQ8YnI+Cgk8c3Ryb25nPlJlYWwgRXN0YXRlIEFnZW50czwvc3Ryb25nPiA8L2E+PC9kaXY+CgoKICAgIDxwPgoJSWYgeW91IGFyZSBhIFJFQUEgbGljZW5zZWQgcmVhbCBlc3RhdGUgYWdlbnQsIG9yIHNhbGVzIHBlcnNvbiwgZmluZCBvdXQgaG93IFByb3BlcnR5IEd1cnUgPGVtPlByb2Zlc3Npb25hbCA8L2VtPmNhbiBoZWxwIGluY3JlYXNlIHlvdXIgbGlzdGluZ3MgYW5kIHNhbGVzIHN1Y2Nlc3MuPC9wPgo8YnI+Cjxicj4KPGJyPgo8cD4KCTxhIGhyZWY9Ii9jb250ZW50L3JlZ2lzdGVyZWQtcmVhbGVzdGF0ZS1hZ2VudHMiPlJlYWQgbW9yZTwvYT48L3A+CjxwPgoJJm5ic3A7PC9wPgoKCgkJCTwvZGl2PjwvZGl2PgoJCQkgPGRpdiBjbGFzcz0iY29sNCI+PGRpdiBjbGFzcz0iYm9yZGVyZWQiPgogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgPGRpdiBjbGFzcz0iYnViYmxlLWdyZWVuIj4KCTxhIGhyZWY9Ii9jb250ZW50L3JlZ2lzdGVyZWQtdmFsdWVycyI+UmVnaXN0ZXJlZDxicj4KCTxzdHJvbmc+VmFsdWVyczwvc3Ryb25nPiA8L2E+PC9kaXY+CgoKICAgIDxwPgoJSWYgeW91IGFyZSBhIHJlZ2lzdGVyZWQgdmFsdWVyLCB5b3UgbmVlZCB0aGUgbGF0ZXN0IHByb3BlcnR5IGluZm9ybWF0aW9uIHRvIHByb3ZpZGUgdXAtdG8tZGF0ZSBhY2N1cmF0ZSBtYXJrZXQgdmFsdWF0aW9uIGFkdmljZS4gRmluZCBvdXQgaG93IFByb3BlcnR5IEd1cnUgPGVtPlByb2Zlc3Npb25hbCA8L2VtPmNhbiBoZWxwIHlvdSBwcm92aWRlIHlvdXIgY2xpZW50cyBhbiBldmVuIGJldHRlciBzZXJ2aWNlLjwvcD4KPHA+Cgk8YSBocmVmPSIvY29udGVudC9yZWdpc3RlcmVkLXZhbHVlcnMiPlJlYWQgbW9yZTwvYT48L3A+CjxwPgoJJm5ic3A7PC9wPgoKCiAgICAgICAgICAgICAgICAgICAgICAgIDwvZGl2PjwvZGl2PgoJCQkgPGRpdiBjbGFzcz0iY29sNCI+PGRpdiBjbGFzcz0iYm9yZGVyZWQiPgogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgPGRpdiBjbGFzcz0iYnViYmxlLXllbGxvdyI+Cgk8YSBocmVmPSIvY29udGVudC9wcm9wZXJ0eS1kZXZlbG9wZXJzIj5Qcm9wZXJ0eTxicj4KCTxzdHJvbmc+RGV2ZWxvcGVyczwvc3Ryb25nPiA8L2E+PC9kaXY+CgoKICAgIDxwPgoJSWYgeW91IGFyZSBhIHByb3BlcnR5IGRldmVsb3BlciwgaGF2aW5nIHRoZSBtb3N0IHVwLXRvLWRhdGUsIGFjY3VyYXRlLCBuYXRpb25hbCBwcm9wZXJ0eSBpbmZvcm1hdGlvbiBhdCB5b3VyIGZpbmdlcnRpcHMgd2lsbCBoZWxwIHlvdSBhbmFseXNlIG9wcG9ydHVuaXRpZXMgYW5kIGdhaW4gY29tcGV0aXRpdmUgYWR2YW50YWdlLjwvcD4KPGJyPgo8YnI+CjxwPgoJPGEgaHJlZj0iL2NvbnRlbnQvcHJvcGVydHktZGV2ZWxvcGVycyI+UmVhZCBtb3JlPC9hPjwvcD4KPHA+CgoKCiAgICAgICAgICAgICAgICAgICAgICAgIDwvcD48L2Rpdj48L2Rpdj4KCQkJIDxkaXYgY2xhc3M9ImNvbDQiPjxkaXYgY2xhc3M9ImJvcmRlcmVkIj4KICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgIDxkaXYgY2xhc3M9ImJ1YmJsZS1yZWQiPgoJPGEgaHJlZj0iL2NvbnRlbnQvcHJvcGVydHktaW52ZXN0b3JzIj5Qcm9wZXJ0eTxicj4KCTxzdHJvbmc+SW52ZXN0b3JzPC9zdHJvbmc+IDwvYT48L2Rpdj4KCgogICAgPHA+CglJZiB5b3UgYXJlIGEgcHJvcGVydHkgaW52ZXN0b3IsIGltbWVkaWF0ZSBhY2Nlc3MgdG8gdGhlIG1vc3QgdXAtdG8tZGF0ZSBhbmQgYWNjdXJhdGUgbmF0aW9uYWwgcHJvcGVydHkgZGF0YXNldCBpbiBOZXcgWmVhbGFuZC4gTGVhcm4gaG93IFByb3BlcnR5IEd1cnUgY2FuIGhlbHAgaW1wcm92ZSB5b3VyIFJPSS48L3A+Cjxicj4KPGJyPgo8cD4KCTxhIGhyZWY9Ii9jb250ZW50L3Byb3BlcnR5LWludmVzdG9ycyI+UmVhZCBtb3JlPC9hPjwvcD4KPHA+CgoKCiAgICAgICAgICAgICAgICAgICAgICAgIDwvcD4KICAgIDwvZGl2PgogIDwvZGl2Pgo8L2Rpdj4KCgk8L2Rpdj4KCTxkaXYgaWQ9Im5ld3Nfc2lkZWJhciIgY2xhc3M9J3NwYW40JyBzdHlsZT0ibWFyZ2luLXRvcDoyMnB4OyI+CgkJCgk8L2Rpdj4KPC9kaXY+Cgo8L2Rpdj4KCjxkaXYgY2xhc3M9J2Zvb3Rlcic+CiAgPHVsIGNsYXNzPSduYXYgbmF2LXBpbGxzIG5hdi1jZW50ZXInPgogICAgPGxpPjxhIGhyZWY9Imh0dHA6Ly93d3cuY29yZWxvZ2ljLmNvLm56L2NvbnRhY3QtdXMvIj5Db250YWN0IHVzPC9hPjwvbGk+CiAgICA8bGk+PGEgaHJlZj0iaHR0cDovL3d3dy5jb3JlbG9naWMuY28ubnovYWRtaW4vcHJpdmFjeS1wb2xpY3kvIj5Qcml2YWN5PC9hPjwvbGk+CiAgICA8bGk+PGEgaHJlZj0iaHR0cDovL3d3dy5wcm9wZXJ0eS1ndXJ1LmNvLm56L2d1cnV4L2hlbHAvQ0xOWl9TdGFuZGFyZF9UZXJtc19hbmRfQ29uZGl0aW9uc19KYW5fMjAxNS5wZGYiIHRhcmdldD0iX2JsYW5rIj5UZXJtcyBhbmQgQ29uZGl0aW9uczwvYT48L2xpPgogIDwvdWw+CiAgPGRpdiBzdHlsZT0iZm9udC1zaXplOjlweDtsaW5lLWhlaWdodDoxMnB4O3RleHQtYWxpZ246Y2VudGVyOyI+Q29weXJpZ2h0IDIwMTQtMjAyNCBDb3JlTG9naWMgTlogTHRkPC9kaXY+CjwvZGl2PgoKPHNjcmlwdCB0eXBlPSJ0ZXh0L2phdmFzY3JpcHQiPgoKKGZ1bmN0aW9uKCkgewptaWNyb0FqYXgoJy9jb250ZW50L3dlbGNvbWVwYWdlL25ld3Nfc2lkZWJhcicsZnVuY3Rpb24ocmVzKXsKICAgIGRvY3VtZW50LmdldEVsZW1lbnRCeUlkKCduZXdzX3NpZGViYXInKS5pbm5lckhUTUwgPSByZXM7Cn0pOwo8L3NjcmlwdD4KPC9ib2R5Pgo8L2h0bWw+Cg=="));
                            await e.RespondAsync(new ResponseData()
                            {
                                Body = guruxcontent,
                                Status = System.Net.HttpStatusCode.OK,
                            });
                        }

                        var result = RegexDenyTracking.Match(e.Url);
                        if (!result.Success)
                        {
                            await e.ContinueAsync();
                        }
                        else
                        {
                            await e.AbortAsync();
                        }
                    }
                    else
                    {
                        await Task.Delay(10);
                    }
                }
                catch { }
            }
        }

        public static void AddRequest(Request request)
        {
            _incoming.Add(request);
        }
    }
}
