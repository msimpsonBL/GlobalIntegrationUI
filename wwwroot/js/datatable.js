jQuery.noConflict();

$(document).ready(function () {
    if (!$.fn.DataTable) {
        console.error('DataTables library is not loaded.');
    } else {
        console.log('DataTables library loaded successfully.');
    }
    // Initialize DataTable and store it in a variable 'table'
    var table = $('#tblData').DataTable({
        deferRender: true,
        processing: true,
        serverSide: true,
        filter: true,
        ajax: {
            type: 'POST',
            url: '/Status/GetData',
            data: function (d) {
                console.log(d);
                d.startDate = $('#startDate').val();
                d.endDate = $('#endDate').val();

                // Check if dates are being sent
                console.log("Start Date: " + d.startDate);
                console.log("End Date: " + d.endDate);
            },
            dataType: 'json', // Ensure data returned is in JSON format
            error: function (xhr, error, code) {
                console.log("Error Code: " + code);
                console.log("Error Message: " + xhr.responseText);
            }
        },
        columns: [
            { "data": "parentEvent.title", "name": "Title", "autowidth": true },
            { "data": "parentEvent.author", "name": "Author", "autowidth": true },
            { "data": "parentEvent.collectionCode", "name": "CollectionCode", "autowidth": true },
            { "data": "parentEvent.creationTime", "name": "CreationTime", "autowidth": true },
            { "data": "parentEvent.eventName", "name": "EventName", "autowidth": true },
            {
                data: null,
                name: "Actions",
                orderable: false,
                render: function (data, type, row, meta) {
                    //    return '<a href="#" class="btn btn-sm btn-danger m-1 p-1">Remove</a>';
                    return `<a href="#" class="btn btn-sm btn-danger m-1 p-1 remove-btn" data-id="${row.parentEvent.eventId}">Remove</a>`;
                }
            }
        ],
        createdRow: function (row, data) {
            $(row).addClass('row-accordion');
        },
        columnDefs: [
            {
                targets: [4],
                visible: false
            }
        ],
        // Highlight rows based on the value of 'eventName'
        rowCallback: function (row, data) {

            // Ensure that parentEvent exists before trying to access its properties
            if (data.parentEvent && data.parentEvent.eventName) {
                if (data.parentEvent.eventName.includes('NewRsiMessagePublished.IntegrationEvent')) {
                    $(row).addClass('event-green');
                } else if (data.parentEvent.eventName.includes('NewRsiMessageReceived.IntegrationEvent')) {
                    $(row).addClass('event-orange');
                } else if (data.parentEvent.eventName.includes('NewRsiMessageSubmitted.IntegrationEvent')) {
                    $(row).addClass('event-blue');
                }
            }
        }
    });

    $('#tblData tbody').on('click', 'tr.row-accordion', function () {
        var tr = $(this);
        var row = table.row(tr);

        if (row.child.isShown()) {
            // Close the row
            row.child.hide();
            tr.removeClass('shown');
        } else {
            // Build child row content dynamically
            var relatedEvents = row.data().relatedEvents;
            if (relatedEvents && relatedEvents.length > 0) {
                var childContent = `
            <div class="accordion" id="accordion-${row.index()}">
            `;
                relatedEvents.forEach(function (event, index) {
                    var formattedEventName = event.parentEvent.eventName ? event.parentEvent.eventName.split('.').slice(0, -1).join('.') : '';
                    // Determine background color based on EventName
                    var headerBackgroundColor = '#93979b'; // Default gray
                    if (event.parentEvent.eventName.includes('NewRsiMessageReceived.IntegrationEvent')) {
                        headerBackgroundColor = '#fd7e14'; // Orange
                    } else if (event.parentEvent.eventName.includes('NewRsiMessageSubmitted.IntegrationEven')) {
                        headerBackgroundColor = '#007bff'; // Blue
                    }
                    childContent += `
            <div class="accordion-item">
                <h2 class="accordion-header" id="heading-${row.index()}-${index}" style="background-color: ${headerBackgroundColor};">
                    <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapse-${row.index()}-${index}" aria-expanded="false" aria-controls="collapse-${row.index()}-${index}">
                        ${formattedEventName} - ${event.identifier}
                    </button>
                </h2>
                <div id="collapse-${row.index()}-${index}" class="accordion-collapse collapse" aria-labelledby="heading-${row.index()}-${index}" data-bs-parent="#accordion-${row.index()}">
                    <div class="accordion-body">
                        <table class="table table-bordered">
                            <tbody>
                                <tr><td><strong>Title:</strong></td><td>${event.parentEvent.title}</td></tr>
                                <tr><td><strong>Author:</strong></td><td>${event.parentEvent.author}</td></tr>
                                <tr><td><strong>Collection Code:</strong></td><td>${event.parentEvent.collectionCode}</td></tr>
                                <tr><td><strong>Creation Time:</strong></td><td>${event.parentEvent.creationTime}</td></tr>
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>`;
                });
                childContent += `</div>`; // Close accordion
                row.child(childContent).show();
                tr.addClass('shown');
            }
        }
    });

    $('#tblData tbody').on('click', '.remove-btn', function (e) {
        e.preventDefault();

        // Get the EventId from the button's data attribute
        var eventId = $(this).data('id');

        // Confirm deletion
        if (!confirm('Are you sure you want to delete this record?')) {
            return;
        }

        // Send an AJAX request to the server to delete the record
        $.ajax({
            type: 'POST',
            url: '/Status/DeleteEvent', // Adjust the endpoint as needed
            data: { eventId: eventId },
            success: function (response) {
                if (response.success) {
                    // Find and remove the parent row
                    var parentRow = table.row($(`.remove-btn[data-id="${eventId}"]`).closest('tr'));
                    var parentData = parentRow.data();

                    if (parentData && parentData.relatedEvents) {
                        // Find and remove child rows
                        parentData.relatedEvents.forEach(function (childEvent) {
                            var childRow = table.rows().data().toArray().find(row => row.parentEvent.eventId === childEvent.eventId);
                            if (childRow) {
                                table.row(function (idx, data) {
                                    return data.parentEvent.eventId === childEvent.eventId;
                                }).remove();
                            }
                        });
                    }

                    // Remove the parent row
                    parentRow.remove().draw();

                    alert('Record deleted successfully.');
                } else {
                    alert('Error: ' + response.message);
                }
            },
            error: function (xhr, status, error) {
                console.error('Error:', error);
                alert('An error occurred while deleting the record.');
            }
        });
    });

    // Handle the date range filter click event
    $('#filterDateRange').on('click', function () {
        table.draw(); // Redraw the table with new filter parameters
    });

    // Handle the reset filter click event
    $('#resetFilter').on('click', function () {
        $('#startDate').val(''); // Reset start date input
        $('#endDate').val('');   // Reset end date input
        table.draw();            // Redraw the table with no filter
    });
});