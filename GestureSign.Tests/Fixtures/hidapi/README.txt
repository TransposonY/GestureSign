HID preparsed data corpus from HIDAPI
======================================

Source: https://github.com/libusb/hidapi/tree/master/windows/test/data
(downloaded 2026-09-29, all 25 device pairs, files unmodified)

  <VID>_<PID>_<Usage>_<UsagePage>_real.rpt_desc  report descriptor as captured by the contributor's tool
                                                 (C array, item listing or hex dump; see HidapiCorpus.cs)
  <VID>_<PID>_<Usage>_<UsagePage>.pp_data        HIDP_PREPARSED_DATA of that top-level collection as
                                                 returned by HidD_GetPreparsedData on real Windows,
                                                 dumped by hidapi's windows/pp_data_dump/pp_data_dump.c

Used by GestureSign.Tests/Hid/PreparsedDataBuilderCorpusTests.cs to validate HidPreparsedDataBuilder.

These files are part of HIDAPI and are redistributed under HIDAPI's BSD-style license
(HIDAPI is dual/triple licensed; the BSD option, LICENSE-bsd.txt, is reproduced below):

--------------------------------------------------------------------------------
Copyright (c) 2010, Alan Ott, Signal 11 Software
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice,
      this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.
    * Neither the name of Signal 11 Software nor the names of its
      contributors may be used to endorse or promote products derived from
      this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
POSSIBILITY OF SUCH DAMAGE.
--------------------------------------------------------------------------------
