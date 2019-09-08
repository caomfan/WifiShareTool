using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace WifiShareTool.ViewModel
{
    public class VmBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void  RaisePropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(name));
        }

        public void RaisePropertyChanged<T>(Expression<Func<T>> expression)
        {
            var name = (expression.Body as MemberExpression).Member.Name;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
