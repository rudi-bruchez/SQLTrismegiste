// -----------------------------------------------------------------------
// <copyright file="AboutControlViewModel.cs" company="">
//
// The MIT License (MIT)
// 
// Copyright (c) 2014 Christoph Gattnar
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of
// the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
// BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
// CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//
// </copyright>
// -----------------------------------------------------------------------

namespace SQLTrismegiste.AboutBox
{
	public class About : AboutControlViewModel
	{
		public void Show()
		{
			AboutControlView about = new AboutControlView();
			AboutControlViewModel vm = (AboutControlViewModel)about.FindResource("ViewModel");
			vm.AdditionalNotes = this.AdditionalNotes;
			vm.ApplicationLogo = this.ApplicationLogo;
			vm.Copyright = this.Copyright;
			vm.Description = this.Description;
			vm.HyperlinkText = this.HyperlinkText;
			vm.Publisher = this.Publisher;
			vm.PublisherLogo = this.PublisherLogo;
			vm.Title = this.Title;
			vm.Version = this.Version;

			vm.Window.Content = about;
			vm.Window.Show();
		}
	}
}
