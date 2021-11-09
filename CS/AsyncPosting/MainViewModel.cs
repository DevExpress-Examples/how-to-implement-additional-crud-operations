﻿using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Xpf;
using EntityFrameworkIssues.Issues;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace AsyncPosting {
    public class MainViewModel : ViewModelBase {
        IssuesContext _Context;
        ObservableCollection<User> _ItemsSource;

        public ObservableCollection<User> ItemsSource {
            get {
                if(_ItemsSource == null && !IsInDesignMode) {
                    _Context = new IssuesContext();
                    _ItemsSource = new ObservableCollection<User>(_Context.Users);
                    _ItemsSource.CollectionChanged += _ItemsSource_CollectionChanged;
                }
                return _ItemsSource;
            }
        }

        private void _ItemsSource_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
        }

        [Command]
        public void ValidateRow(RowValidationArgs args) {
            var item = (User)args.Item;
            args.ResultAsync = Task.Run(async () => {
                await Task.Delay(3000);
                if(args.IsNewItem) {
                    _Context.Users.Add(item);
                } else {
                    _Context.SaveChanges();
                }
                //throw new Exception("dangerous");
                return new ValidationErrorInfo("fdsfdsf");
            });
            //args.Result = "ffs;";
        }
        [Command]
        public void ValidateRowDeletion(ValidateRowDeletionArgs args) {
            Task.Delay(3000).Wait();
            var item = (User)args.Items.Single();
            _Context.Users.Remove(item);
            _Context.SaveChanges();
        }
        [Command]
        public void DataSourceRefresh(DataSourceRefreshArgs args) {
            args.RefreshAsync = Task.Run(async () => {
                await Task.Delay(3000);
                _ItemsSource = null;
                _Context = null;
                RaisePropertyChanged(nameof(ItemsSource));
            });
        }
    }
}
