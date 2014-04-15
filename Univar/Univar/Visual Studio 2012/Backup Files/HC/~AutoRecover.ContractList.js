/// <reference path="../Scripts/jquery-1.8.3.js" />
/// <reference path="../Scripts/jquery-ui-1.9.2.js" />
/// <reference path="../Scripts/knockout.debug.js" />
/// <reference path="../Scripts/underscore.js" />
/// <reference path="../Scripts/underscore-ko-1.1.0.js" />
/// <reference path="../Scripts/inputMask/jquery.inputmask.date.extensions.js" />
/// <reference path="../Scripts/inputMask/jquery.inputmask.extensions.js" />
/// <reference path="../Scripts/inputMask/jquery.inputmask.js" />
/// <reference path="../Scripts/inputMask/jquery.inputmask.numeric.extensions.js" />
/// <reference path="../Scripts/helpers.js" />
/// <reference path="../Scripts/emulatetab.joelpurra.js" />
/// <reference path="../Scripts/jquery.placeholder.js" />
/// <reference path="../Scripts/jquery.numberformatter-1.2.3.min.js" />
/// <reference path="../Scripts/knockout.mapping-latest.debug.js" />
/// <reference path="../Scripts/toastr.js" />
/// <reference path="../Scripts/knockout.asyncCommand.js" />
/// <reference path="../Scripts/knockout.activity.js" />
/// <reference path="../Scripts/knockout-classBindingProvider.js" />
/// <reference path="../Scripts/knockback.js" />
/// <reference path="../Scripts/knockback-inspector.js" />
/// <reference path="../Scripts/backbone.js" />
/// <reference path="../Scripts/koGrid.debug.js" />
/// <reference path="../Scripts/koGrid-reorderable.js" />
/// <reference path="../Scripts/DataTables-1.9.4/media/js/jquery.dataTables.js" />
/// <reference path="../Scripts/q.js" />
/// <reference path="../Scripts/breeze.debug.js" />

var _dateMaskFormat = 'yyyy/mm/dd';
var _dateFormat = 'yy//mm/dd';
var _now = new Date().toISOString();// $.datepicker.formatDate(_dateFormat, new Date());

//$.datepicker.setDefaults($.datepicker.regional['en-GB']);
$.datepicker.setDefaults({
    dateFormat: _dateFormat,
    changeYear: true,
    changeMonth: true,
    autoSize: true,
    gotoCurrent: false,
    numberOfMonths: [1, 2], showCurrentAtPos: 0,
    showAnim: 'drop',
    //showButtonPanel: true,
});

var HCContract = function () {
    this.Id = c.Id;
    this.Active; //= ko.observable();
    this.DateAgreed;// = ko.observable();
    this.DateInput;// = ko.observable();
    this.CashPrice;// = ko.observable();
    this.DownPayment;// = ko.observable();
    this.Balance;// = ko.observable();
    this.StaffName;// = ko.observable();
    this.ClientName;//= ko.observable();
    this.ProductList;// = ko.observable();
}
function extendHCContract(base) {
    var self = base || new HCContract();
    self.Id = ko.observable(self.Id).setInfoUrl('contract.aspx');
    return self;
}

function tmplEdit() {
    return '<div data-bind="kgCell: $cell">'
  + '<input type="text" readonly="readonly" data-bind="value: $data.getProperty($parent)" style="width: 120px; border: 0;" />'
  + '</div>';
}
function tmplTitled() {
    return 'div data-bind="kgCell: $cell">'
    + '<label class="cPadSides" data-bind="text: $data.getProperty($parent), attr: { title: $data.getProperty($parent) }"></label>'
    + '</div>';
}
function tmplNumeric() {
    return 'span class="$cellClass" data-bind="kgCell: $cell">'
    + '<label class="cPadSides alignRight" data-bind="text: formatAmount($data.getProperty($parent), 0)"></label>'
    + '</span>';
}
function tmplCheckbox() {
    return 'div class="alignMiddle alignCenter cPad3 $cellClass" data-bind="kgCell: $cell">'
    + '<label class="cBold" data-bind="text: $data.getProperty($parent) ? \'&#10003;\' : \'X\'"></label>'
    + '</div>';
}
function tmplDate() {
    return 'span class="alignRight $cellClass" data-bind="kgCell: $cell">'
    + '    <label class="cPadSides" data-bind="text: formatDate($data.getProperty($parent))"></label>'
    + '</span>';
}
function tmplLinkedId(url) {
    var command = "'javascript:window.open(\\'" + url + "' + $data.getProperty($parent) + '\\', \\'_blank\\'); return false;'";
    return 'span class="alignRight $cellClass" data-bind="kgCell: $cell">'
    + '<a data-bind="attr: { onmousedown: \'javascript'}" class="cHand">'
    + '<label class="cPadSides" data-bind="text: $data.getProperty($parent)"></label>'
    + '</a>'
    + '</span>';
}

function tmpdEditable() {
    return '<div data-bind="if: $root.selected, kgCell: $cell" class="$cellClass">'
    + ' <input type="text" data-bind="value: $data.getProperty($parent)"/>'
    + '</div>'
    //+ '<div data-bind="ifnot: $root.selected, kgCell: $cell" class="$cellClass">'
    // + ' <span data-bind="text: $data.getProperty($parent)"></span>'
    //+ '</div>'
    ;
}

var ViewModel = function () {
    var self = this;
    this.contractsPage = ko.observable();
    this.openContract = function (url) {
        window.open('contracts.aspx?contractId=' + $data.getProperty($parent), '_blank');
        return false;
    }
    this.params = {
        dateStart: ko.observable(''),
        dateEnd: ko.observable(''),
        isDateAgreed: ko.observable(false),
        query: ko.observable('dec'),
        currentPage: ko.observable(1),
        pageSize: ko.observable(50),
        totalServerItems: ko.observable(258),
        sortInfo: ko.observable(''),
        filterInfo: ko.observable(''),
    }

    this.cList = ko.observableArray([]);

    this.contractList = ko.observableArray([]);//.extend({ 'throttle': 150 });
    ko.watch(this.params, function (params, modProp) {
        getContractList(ko.toJS(self.params), self);
    });

    this.selectedContracts = ko.observableArray([]);
    this.balance = ko.computed(function () {
        var balance = 0;
        $.each(self.selectedContracts() || [], function () { balance += this.Balance; });
        return formatAmount(balance, 0);
    });

    // *** koGrid ***

    this.columnDefinitions = ko.observableArray([
        { field: 'Id', width: 75, displayName: 'ID', cellTemplate: tmplLinkedId('contracts.aspx?contractId=') },
        { field: 'Active', width: 35, displayName: 'Ac.', cellTemplate: tmplCheckbox() },
        { field: 'DateInput', width: 95, displayName: 'Dt.Input', cellTemplate: tmplDate() },
        { field: 'DateAgreed', width: 95, displayName: 'Dt.Agreed', cellTemplate: tmplDate() },
        { field: 'CashPrice', width: 80, displayName: 'Cs.Price', cellTemplate: tmplNumeric() },
        { field: 'DownPayment', width: 80, displayName: 'Dn.Paym.', cellTemplate: tmplNumeric() },
        { field: 'Balance', width: 80, displayName: 'Balance', cellTemplate: tmplNumeric() },
        { field: 'StaffName', width: 150, displayName: 'Staff', cellTemplate: tmplTitled() },
        { field: 'ClientName', width: 150, displayName: 'Client', cellTemplate: tmplTitled() },
        { field: 'Products', width: '100%', displayName: 'Products', cellTemplate: tmplTitled() },
    ]);

    this.koGridBindings = {
        data: "contractList",
        columnDefs: "columnDefinitions",
        autogenerateColumns: false,
        isMultiSelect: true,
        selectedItems: "selectedContracts",
        //selectedItem:"selectedContract",
        footerTemplate: undefined,
        showGroupPanel: true,
        jqueryUIDraggable: true,
        autogenerateColumns: false,
        isMultiSelect: false,
        enablePaging: true,
        useExternalFiltering: false,
        useExternalSorting: false,
        filterInfo: "params.filterInfo",
        sortInfo: "params.sortInfo",
        pageSize: "params.pageSize",
        pageSizes: [25, 50, 75],
        currentPage: "params.currentPage",
        totalServerItems: "params.totalServerItems"
    }

    //screenSaverTimeout(180);
};


var vm = new ViewModel();

function getContractList(params, ctx) {
    ctx.contractList([]);
    //if (!params.currentpage || !params.pageSize)
    //    return;
    if (params.query.length >= 3 || params.query == '*') {
        if (params.query == '*') {
            if (params.dateStart.indexOf('yy') > -1)
                return
            params.query = '';
        }
        //getContractList(pageSize, page, filterInfo, sortInfo);

        PostData('GetContractList', params, true, function (e) {
            var matcher;
            try { matcher = new RegExp(params.query, 'i'); } catch (e) { }
            var results = ko.utils.parseJson(e.d);
            //var filteredData = koFilter(results, params.filterInfo);
            //var sortedData = koSort(filteredData, params.sortInfo);
            //var pagedData = sortedData.slice((params.currentpage - 1) * params.pageSize, params.currentpage * params.pageSize);

            $.each(results, function () {
                if (this.MatchedProduct && this.Products.length > 1) {
                    for (var i = 0; i < this.Products.length; i++)
                        if (this.Products[i].match(matcher)) {
                            this.Products.unshift(this.Products[i]);
                            this.Products.splice(i + 1, 1);
                        }
                    this.Products = this.Products.join(', ');
                }
            });
            //ko.utils.arrayPushAll(ctx.contractList, results); //self.purchaseOrderItems.valueHasMutated();
            ctx.contractList(results);
        });
    }
}

$(function () {
    //$('input, textarea').placeholder();
    /*var bindings = {
        fadeInOn: function(ctx) {
            return vm.HCStoreProduct.HCProduct.Id ;
        }
    } 
    ko.bindingProvider.instance = new ko.classBindingProvider(bindings);*/

    //$.get("/api/ProductsData/Products", function (data) {
    //    $("#divToRenderTo").html(data);
    //});

    //$.extend($.fn.dataTableExt.oStdClasses, { "sWrapper": "dataTables_wrapper form-inline" });

    var dt = $('#dt').hide();
    //dt.attr('data-bind', 'dataTable: { ' + 'dataSource: contractList, ' + 'columns: dtColumnDefs, options: dtOptions, sDom: dtsDom }');

    //vm.koGridBindings.footerTemplate = "'" + $('#footerContractDetails').html() + "'";
    bindings = JSON.stringify(vm.koGridBindings).replace(/\"/g, " ");
    $('#koGrid').attr('data-bind', 'koGrid : ' + bindings);

    ko.applyBindings(vm);

    //dt.columnFilter();

    $('input[id*=Date]').datepicker().inputmask(_dateMaskFormat);
    $('.money').inputmask('999,999,999.99', { numericInput: true });

    $(document).on('keydown', 'input,textarea,select', function (ev) {
        if (ev.keyCode == 9)
            setTimeout(function () { // allow processing on blur events before proceeding.
                findFirstInputInNextParent(ev.srcElement, 'tr', '.focusOnTab').focus();
            }, 0);
    });

    activateSelectOnFocus();

    Placeholders.init({ live: false, hideOnFocus: true /*hide the placeholder when the element receives focus*/ });
    //$("#form").enterAsTab({ 'allowSubmit': true }); // can cause next lines to fail if not exists when published
});

var koFilter = function (data, filterInfo) {
    var mgr = new kg.FilterManager({ data: ko.observableArray(data) });
    mgr.filterInfo(filterInfo);
    return mgr.filteredData();
};

var koSort = function (data, sortInfo) {
    var mgr = new kg.SortManager({ data: ko.observableArray(data) });
    mgr.sortInfo(sortInfo);
    return mgr.sortedData();
};

/*function getProducts(elem) {
    var newValue = $(elem).val();
    postDataAsync(location.pathname + '.aspx/GetProducts', { excelFilePath: newValue }, function (e) {
        var storeProducts = ko.mapping.fromJS(e.d)();
        vm.HCStoreProducts(storeProducts);
    });
}*/



// *** Datatables ***

//this.dtColumnDefs = [
//    'Id',
//    'Active',
//    'DateInput',
//    'DateAgreed',
//    'CashPrice',
//    'DownPayment',
//    'Balance',
//    'StaffName',
//    'ClientName',
//    'Products'
//];
//this.dtsDom = "<'row'<'span6'l><'span6'f>r>t<'row'<'span6'i><'span6'p>>";
//this.dtOptions = {
//    aLengthMenu: [[50, 250, 1000, -1], [50, 250, 1000, "All"]],
//    aLength: 250,
//    //sScrollY: '100%',
//    bServerSide: true,
//    bProcessing: true,
//    sAjaxSource: 'http://localhost:59069/contractlist.aspx/AjaxData',
//    //bJQueryUI: true,
//    //rowTemplate: 'UserListRowTemplate',
//    sPaginationType: 'bootstrap',// "full_numbers",
//    fnRowCallback: function onRowCallback(row, data, displayIndex, displayIndexFull) {
//        return $(row).is(".odd") ? $(row).css("color", "red")[0] : row;
//    },
//    fnFilterComplete: function (a, b, index, d) {
//    }//,
//    /*fnServerData: function (sSource, aoData, fnCallback) {
//        //return;
//        if (echo == 1) {
//            var param = { xx: 'test', sortColumnIndex: 22 };
//            $.each(aoData, function (a, b) { param[b.name] = b.value; });
//            echo++;
//            PostData('AjaxData', { param: param }, true, function (e) {
//                var matcher;
//                try { matcher = new RegExp(params.query, 'i'); } catch (e) { }
//                var results = ko.utils.parseJson(e.d);
//                $.each(results.aaData, function () {
//                    if (this.MatchedProduct && this.Products.length > 1) {
//                        for (var i = 0; i < this.Products.length; i++)
//                            if (this.Products[i].match(matcher)) {
//                                this.Products.unshift(this.Products[i]);
//                                this.Products.splice(i + 1, 1);
//                            }
//                        this.Products = this.Products.join(', ');
//                    }

//                });
//                //ko.utils.arrayPushAll(ctx.contractList, results); //self.purchaseOrderItems.valueHasMutated();
//                //self.contractList(results);
//                setTimeout(function () { echo = 1; }, 500);
//                //fnCallback.call(results, results.aaData);
//            });
//        }
//    }*/
//};
//var echo = 1;