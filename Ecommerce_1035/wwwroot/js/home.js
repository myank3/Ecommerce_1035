(function ($) {
    'use strict';

    if (!$) return;

    const TABLE_SELECTOR = '#tblData';
    const FILTER_TYPE_SELECTOR = '#filterType';
    const SEARCH_BTN_SELECTOR = '#btnSearch';
    const SEARCH_INPUT_SELECTOR = `${TABLE_SELECTOR}_filter input`;

    let table = null;
    let searchFilterFn = null;

    /**
     * Custom search predicate for title/author filtering.
     * Reads term + selected filter type each draw.
     */
    function buildSearchFilter() {
        return function (settings, searchData, index) {
            if (settings.sTableId !== 'tblData') return true;

            const term = ($(SEARCH_INPUT_SELECTOR).val() || '').trim().toLowerCase();
            if (!term) return true;

            const row = table.row(index).node();
            const $row = row ? $(row) : $();

            const title = String(
                $row.data('title') ||
                $row.find('[data-title]').data('title') ||
                ''
            ).toLowerCase();

            const author = String(
                $row.data('author') ||
                $row.find('[data-author]').data('author') ||
                ''
            ).toLowerCase();

            const type = ($(FILTER_TYPE_SELECTOR).val() || 'all');

            switch (type) {
                case 'title': return title.includes(term);
                case 'author': return author.includes(term);
                default: return title.includes(term) || author.includes(term);
            }
        };
    }

    /**
     * Replace DataTables' default "Search:" label + input with
     * a Bootstrap input-group: [type select] [input] [button].
     */
    function decorateFilterBox() {
        const $filter = $(`${TABLE_SELECTOR}_filter`);
        if (!$filter.length) return;

        // Remove stray text nodes (the "Search:" label)
        $filter.contents()
            .filter(function () { return this.nodeType === 3; })
            .remove();

        const $input = $filter.find('input').off();

        if (!$input.parent().hasClass('input-group')) {
            $input.addClass('form-control')
                .attr({
                    placeholder: 'Search...',
                    'aria-label': 'Search books'
                })
                .css('max-width', '220px')
                .wrap('<div class="input-group d-inline-flex w-auto align-middle"></div>')
                .after(
                    `<button class="btn btn-primary" id="btnSearch" type="button" aria-label="Search">
                          <i class="fas fa-search" aria-hidden="true"></i>
                       </button>`
                );
        }

        // Insert the filter-type select before the input group (once)
        if (!$(FILTER_TYPE_SELECTOR).length) {
            $input.parent().before(`
                <select id="filterType"
                        class="form-select d-inline-block w-auto me-2"
                        style="height:38px;"
                        aria-label="Filter field">
                    <option value="all">Show All</option>
                    <option value="title">Title</option>
                    <option value="author">Author</option>
                </select>
            `);
        }

        // Wire events once (off first, in case init runs twice)
        $input.off('keyup.dtSearch').on('keyup.dtSearch', function (e) {
            if (e.key === 'Enter') {
                table.draw();
            }
        });

        $filter.off('click.dtSearch change.dtSearch')
            .on('click.dtSearch', SEARCH_BTN_SELECTOR, () => table.draw())
            .on('change.dtSearch', FILTER_TYPE_SELECTOR, () => table.draw());
    }

    function initDataTable() {
        const $table = $(TABLE_SELECTOR);
        if (!$table.length || !$.fn.DataTable) return;

        table = $table.DataTable({
            order: [],
            pageLength: 4,
            lengthMenu: [4, 8, 12, 16],
            autoWidth: false,
            responsive: true,
            language: {
                search: '',
                searchPlaceholder: 'Search...',
                lengthMenu: '_MENU_ per page',
                info: 'Showing _START_–_END_ of _TOTAL_',
                infoEmpty: 'No books to show',
                zeroRecords: 'No matching books found'
            },
            columnDefs: [
                { targets: '_all', className: 'align-middle' }
            ],
            initComplete: decorateFilterBox
        });

        // Register custom search predicate (once)
        searchFilterFn = buildSearchFilter();
        $.fn.dataTable.ext.search.push(searchFilterFn);
    }

    /**
     * Clean up when navigating away (SPA-style hosts, Turbo, etc.)
     */
    function destroy() {
        if (searchFilterFn && $.fn.dataTable) {
            const idx = $.fn.dataTable.ext.search.indexOf(searchFilterFn);
            if (idx !== -1) $.fn.dataTable.ext.search.splice(idx, 1);
            searchFilterFn = null;
        }
        if (table) {
            table.destroy();
            table = null;
        }
    }

    $(function () {
        initDataTable();
    });

    // Expose for debugging / manual teardown
    window.homePage = { destroy };

})(window.jQuery);